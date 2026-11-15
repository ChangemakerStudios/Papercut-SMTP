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
using MimeKit;
using Moq;
using NUnit.Framework;
using Papercut.Core.Domain.Message;
using Papercut.Message;

namespace Papercut.Message.Tests;

[TestFixture]
public class MessageSearchTests
{
    private static MimeMessage Message(string subject = "Your order shipped", string? cc = null)
    {
        var message = new MimeMessage { Subject = subject };
        message.From.Add(new MailboxAddress("Shop Bot", "orders@shop.example"));
        message.To.Add(new MailboxAddress("Jane Doe", "jane@customer.example"));
        if (cc != null) message.Cc.Add(MailboxAddress.Parse(cc));
        return message;
    }

    [TestCase("shipped")]
    [TestCase("SHIPPED")]
    [TestCase("orders@shop")]
    [TestCase("Shop Bot")]
    [TestCase("jane@customer.example")]
    [TestCase("doe")]
    public void Matches_subject_sender_and_recipient(string search)
    {
        MessageSearch.Matches(Message(), search).Should().BeTrue();
    }

    [Test]
    public void Matches_cc()
    {
        MessageSearch.Matches(Message(cc: "audit@shop.example"), "audit").Should().BeTrue();
    }

    [Test]
    public void Every_word_must_match_but_not_in_the_same_field()
    {
        MessageSearch.Matches(Message(), "jane shipped").Should().BeTrue();
        MessageSearch.Matches(Message(), "jane refunded").Should().BeFalse();
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void Blank_matches_everything(string? search)
    {
        MessageSearch.Matches(Message(), search).Should().BeTrue();
        MessageSearch.Matches(null, search).Should().BeTrue();
    }

    [Test]
    public void Unreadable_message_never_matches_a_search()
    {
        MessageSearch.Matches(null, "anything").Should().BeFalse();
    }

    [Test]
    public async Task FilterAsync_keeps_order_and_drops_entries_that_fail_to_load()
    {
        var entries = new[] { "a.eml", "b.eml", "c.eml", "d.eml" }.Select(f => new MessageEntry(f)).ToArray();

        var loader = new Mock<IMimeMessageLoader>();
        loader.Setup(l => l.GetAsync(entries[0], It.IsAny<CancellationToken>())).ReturnsAsync(Message("Invoice 1"));
        loader.Setup(l => l.GetAsync(entries[1], It.IsAny<CancellationToken>())).ReturnsAsync(Message("Welcome"));
        loader.Setup(l => l.GetAsync(entries[2], It.IsAny<CancellationToken>())).ThrowsAsync(new IOException("gone"));
        loader.Setup(l => l.GetAsync(entries[3], It.IsAny<CancellationToken>())).ReturnsAsync(Message("Invoice 2"));

        var result = await MessageSearch.FilterAsync(entries, loader.Object, "invoice");

        result.Should().Equal(entries[0], entries[3]);
    }

    [Test]
    public async Task FilterAsync_without_a_search_loads_nothing()
    {
        var entries = new[] { new MessageEntry("a.eml") };
        var loader = new Mock<IMimeMessageLoader>(MockBehavior.Strict);

        var result = await MessageSearch.FilterAsync(entries, loader.Object, " ");

        result.Should().Equal(entries);
    }
}
