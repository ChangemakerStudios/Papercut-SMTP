// Papercut
//
// Copyright © 2008 - 2012 Ken Robertson
// Copyright © 2013 - 2026 Jaben Cargman
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.


using AwesomeAssertions;
using Microsoft.Data.Sqlite;
using MimeKit;
using NUnit.Framework;
using Papercut.Core.Domain.Message;
using Papercut.Message.Index;
using Serilog;

namespace Papercut.Message.Tests;

[TestFixture]
public class MessageIndexTests
{
    private string _folder = null!;

    private string _database = null!;

    private readonly List<MessageIndex> _indexes = [];

    [SetUp]
    public void SetUp()
    {
        _folder = Path.Combine(Path.GetTempPath(), "papercut-index-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_folder);
        _database = Path.Combine(_folder, MessageIndex.FileName);
    }

    [TearDown]
    public void TearDown()
    {
        _indexes.ForEach(i => i.Dispose());
        _indexes.Clear();
        SqliteConnection.ClearAllPools();

        try
        {
            Directory.Delete(_folder, true);
        }
        catch (IOException)
        {
            // best effort
        }
    }

    private MessageIndex Open()
    {
        var index = new MessageIndex(_database, new LoggerConfiguration().CreateLogger());
        _indexes.Add(index);
        return index;
    }

    private string Write(
        string name,
        string subject = "Hello",
        string to = "jane@customer.example",
        string? text = "Plain body",
        string? html = null,
        string? attachment = null)
    {
        var message = new MimeMessage { Subject = subject };
        message.From.Add(new MailboxAddress("Shop Bot", "orders@shop.example"));
        message.To.Add(MailboxAddress.Parse(to));

        var body = new BodyBuilder { TextBody = text, HtmlBody = html };
        if (attachment != null) body.Attachments.Add(attachment, "attachment content"u8.ToArray());
        message.Body = body.ToMessageBody();

        var file = Path.Combine(_folder, name + ".eml");
        message.WriteTo(file);
        return file;
    }

    private List<MessageEntry> Entries() =>
        Directory.GetFiles(_folder, "*.eml").Select(f => new MessageEntry(f)).ToList();

    private async Task<MessageIndex> Built()
    {
        var index = Open();
        await index.SyncAsync(Entries());
        return index;
    }

    private static async Task<List<string>> Find(MessageIndex index, string search) =>
        (await index.SearchAsync(search)).Select(Path.GetFileNameWithoutExtension).OrderBy(n => n).ToList()!;

    [Test]
    public async Task Finds_text_anywhere_in_a_message()
    {
        Write("subject", subject: "Quarterly invoice");
        Write("address", to: "billing-team@acme.example");
        Write("text", text: "Your code is 845213");
        Write("html", text: null, html: "<p>Click <b>confirm</b> below</p><style>.secret{}</style>");
        Write("link", text: null, html: "<a href=\"https://app.example/reset?token=abc123xyz\">Reset</a>");
        Write("both", text: "See the link", html: "<a href=\"https://app.example/verify?t=qq77zz\">Verify</a>");
        Write("attachment", attachment: "statement-2026.pdf");

        var index = await Built();

        (await Find(index, "invoice")).Should().Equal("subject");
        (await Find(index, "acme.exam")).Should().Equal("address");
        (await Find(index, "845213")).Should().Equal("text");
        (await Find(index, "click confirm")).Should().Equal("html");
        (await Find(index, "secret")).Should().BeEmpty("style blocks are not text");
        (await Find(index, "abc123xyz")).Should().Equal("link");
        // links in the HTML part count when there is a text part too
        (await Find(index, "qq77zz")).Should().Equal("both");
        (await Find(index, "statement-2026")).Should().Equal("attachment");
    }

    [Test]
    public async Task Every_word_must_match_and_case_is_ignored()
    {
        Write("a", subject: "Welcome Jane", to: "jane@customer.example");
        Write("b", subject: "Welcome Bob", to: "bob@customer.example");

        var index = await Built();

        (await Find(index, "WELCOME jane@")).Should().Equal("a");
        (await Find(index, "welcome")).Should().Equal("a", "b");
        (await Find(index, "welcome nobody")).Should().BeEmpty();
    }

    [Test]
    public async Task Words_under_three_characters_match_sender_recipient_and_subject()
    {
        Write("a", subject: "PR 42 merged");
        Write("b", subject: "Nothing here", text: "PR 42 is mentioned only in the body");

        var index = await Built();

        (await Find(index, "42")).Should().Equal("a");
    }

    [Test]
    public async Task Short_words_beside_a_longer_one_do_not_narrow_the_search()
    {
        Write("a", subject: "Hello", text: "Just saying hi");

        var index = await Built();

        (await Find(index, "saying hi")).Should().Equal("a");
    }

    [Test]
    public async Task Quotes_and_operators_in_a_search_are_just_text()
    {
        Write("a", subject: "Say \"hello\" AND goodbye");

        var index = await Built();

        (await Find(index, "\"hello\"")).Should().Equal("a");
        (await Find(index, "AND OR NOT")).Should().BeEmpty();
    }

    [Test]
    public async Task Summaries_come_from_the_index()
    {
        var file = Write("a", subject: "Row subject", attachment: "x.pdf");

        var index = await Built();
        var summaries = await index.GetSummariesAsync(Entries());

        var summary = summaries[file];
        summary.Subject.Should().Be("Row subject");
        summary.From.Should().Equal(new IndexedAddress("Shop Bot", "orders@shop.example"));
        summary.AttachmentCount.Should().Be(1);
        summary.Priority.Should().Be("Normal");
    }

    [Test]
    public async Task First_build_starts_read_and_later_mail_is_unread_until_opened()
    {
        var old = Write("old");
        var index = await Built();

        var fresh = Write("fresh");
        await index.SyncAsync(Entries());

        index.IsRead(old).Should().BeTrue();
        index.IsRead(fresh).Should().BeFalse();

        index.MarkRead(fresh);
        index.IsRead(fresh).Should().BeTrue();
    }

    [Test]
    public async Task Read_status_survives_a_restart()
    {
        Write("old");
        var first = await Built();

        var fresh = Write("fresh");
        var other = Write("other");
        await first.SyncAsync(Entries());
        first.MarkRead(fresh);
        first.Dispose();
        SqliteConnection.ClearAllPools();

        var reopened = Open();

        reopened.IsBuilt.Should().BeTrue();
        reopened.IsRead(fresh).Should().BeTrue();
        reopened.IsRead(other).Should().BeFalse();
    }

    [Test]
    public async Task Mark_all_read()
    {
        Write("old");
        var index = await Built();
        var a = Write("a");
        var b = Write("b");
        await index.SyncAsync(Entries());

        index.MarkAllRead();

        index.IsRead(a).Should().BeTrue();
        index.IsRead(b).Should().BeTrue();
    }

    [Test]
    public async Task Opened_before_it_was_indexed_still_counts()
    {
        Write("old");
        var index = await Built();
        var fresh = Write("fresh");

        index.MarkRead(fresh);
        await index.SyncAsync(Entries());

        index.IsRead(fresh).Should().BeTrue();
    }

    [Test]
    public async Task A_file_edited_on_disk_is_reindexed_and_keeps_its_read_status()
    {
        Write("old");
        var index = await Built();
        var file = Write("edited", subject: "Before");
        await index.SyncAsync(Entries());
        index.MarkRead(file);

        Write("edited", subject: "After the edit");
        File.SetLastWriteTimeUtc(file, DateTime.UtcNow.AddMinutes(1));
        await index.SyncAsync(Entries());

        (await Find(index, "before")).Should().BeEmpty();
        (await Find(index, "after the edit")).Should().Equal("edited");
        index.IsRead(file).Should().BeTrue();
    }

    [Test]
    public async Task Deleted_files_drop_out()
    {
        var gone = Write("gone", subject: "Temporary");
        var index = await Built();

        File.Delete(gone);
        await index.SyncAsync(Entries());

        (await Find(index, "temporary")).Should().BeEmpty();
        (await index.GetSummariesAsync([new MessageEntry(gone)])).Should().BeEmpty();
    }

    [Test]
    public async Task Summaries_index_a_page_on_the_spot()
    {
        Write("old");
        var index = await Built();
        var fresh = Write("fresh", subject: "Just arrived");

        var summaries = await index.GetSummariesAsync([new MessageEntry(fresh)]);

        summaries[fresh].Subject.Should().Be("Just arrived");
        summaries[fresh].IsRead.Should().BeFalse();
    }

    [Test]
    public async Task A_corrupt_index_is_rebuilt()
    {
        Write("a", subject: "Survivor");
        await File.WriteAllTextAsync(_database, "this is not a database");

        var index = await Built();

        index.IsAvailable.Should().BeTrue();
        (await Find(index, "survivor")).Should().Equal("a");
    }

    [Test]
    public void An_index_that_cannot_be_created_is_unavailable_not_fatal()
    {
        // a file where the index's folder should be
        var blocker = Path.Combine(_folder, "blocker");
        File.WriteAllText(blocker, "");
        _database = Path.Combine(blocker, MessageIndex.FileName);

        var index = Open();

        index.IsAvailable.Should().BeFalse();
        index.IsRead("anything").Should().BeTrue();
    }

    [Test]
    public void Network_folders_keep_their_index_locally()
    {
        MessageIndex.LocationFor(@"\\server\share\mail", @"C:\local")
            .Should().Be(Path.Combine(@"C:\local", MessageIndex.FileName));

        MessageIndex.LocationFor(_folder, @"C:\local")
            .Should().Be(Path.Combine(_folder, MessageIndex.FileName));
    }

    [Test]
    public void Html_becomes_searchable_text()
    {
        MessageIndexDocument.HtmlToText("<head><title>x</title></head><p>Hi&nbsp;<b>there</b></p><script>var a;</script>")
            .Should().Be("Hi there");
    }
}
