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

namespace Papercut.Message.Tests;

[TestFixture]
public class MessageComposerTests
{
    [Test]
    public void Compose_Markdown_RendersHtmlAndKeepsSourceAsText()
    {
        var message = MessageComposer.Compose(new MessageDraft { Subject = "Build", Markdown = "# Done\n\n| a |\n|---|\n| b |" });

        message.HtmlBody.Should().Contain("<h1").And.Contain("<table>");
        message.TextBody.Should().StartWith("# Done");
    }

    [Test]
    public void Compose_Html_HasNoTextPart()
    {
        var message = MessageComposer.Compose(new MessageDraft { Html = "<b>hi</b>" });

        message.HtmlBody.Should().Be("<b>hi</b>");
        message.TextBody.Should().BeNull();
    }

    [Test]
    public void Compose_Text_HasNoHtmlPart()
    {
        var message = MessageComposer.Compose(new MessageDraft { Text = "hi" });

        message.TextBody.Should().Be("hi");
        message.HtmlBody.Should().BeNull();
    }

    [Test]
    public void Compose_TextWithMarkdown_UsesTextForTheTextPart()
    {
        var message = MessageComposer.Compose(new MessageDraft { Text = "plain", Markdown = "**rich**" });

        message.TextBody.Should().Be("plain");
        message.HtmlBody.Should().Contain("<strong>rich</strong>");
    }

    [Test]
    public void Compose_SubjectOnly_IsAllowedForSimpleNotifications()
    {
        var message = MessageComposer.Compose(new MessageDraft { Subject = "Deploy finished" });

        message.Subject.Should().Be("Deploy finished");
    }

    [Test]
    public void Compose_Defaults_FromPapercut()
    {
        var message = MessageComposer.Compose(new MessageDraft { Subject = "s" });

        message.From.Mailboxes.Single().Address.Should().Be("papercut@localhost");
        message.To.Count.Should().Be(0);
        message.MessageId.Should().NotBeNullOrEmpty();
    }

    [Test]
    public void Compose_Addresses_AreParsedIncludingNames()
    {
        var message = MessageComposer.Compose(new MessageDraft
        {
            Subject = "s",
            From = "Build Bot <build@example.com>",
            To = "a@example.com, B <b@__>",
            Cc = "c@example.com"
        });

        message.From.Mailboxes.Single().Name.Should().Be("Build Bot");
        message.To.Mailboxes.Select(m => m.Address).Should().Equal("a@example.com", "b@__");
        message.Cc.Mailboxes.Single().Address.Should().Be("c@example.com");
    }

    [Test]
    public void Compose_HtmlAndMarkdown_Throws()
    {
        var act = () => MessageComposer.Compose(new MessageDraft { Html = "<b>x</b>", Markdown = "x" });

        act.Should().Throw<ArgumentException>();
    }

    [Test]
    public void Compose_Empty_Throws()
    {
        var act = () => MessageComposer.Compose(new MessageDraft { From = "a@example.com" });

        act.Should().Throw<ArgumentException>();
    }

    [Test]
    public void Compose_BadFrom_Throws()
    {
        var act = () => MessageComposer.Compose(new MessageDraft { Subject = "s", From = "a@b, c@d" });

        act.Should().Throw<ArgumentException>();
    }

    [Test]
    public async Task SaveDraftAsync_HandsTheParsedMessageAndRecipientsToTheHandler()
    {
        byte[]? saved = null;
        string[]? recipients = null;

        var handler = new Mock<IReceivedDataHandler>();
        handler.Setup(h => h.HandleReceivedAsync(It.IsAny<byte[]>(), It.IsAny<string[]>(), true))
            .Callback<byte[], string[], bool>((data, to, _) =>
            {
                saved = data;
                recipients = to;
            })
            .ReturnsAsync("file.eml");

        var file = await handler.Object.SaveDraftAsync(new MessageDraft { Subject = "Ünicode", To = "a@example.com", Cc = "c@example.com", Markdown = "**x**" });

        file.Should().Be("file.eml");
        recipients.Should().Equal("a@example.com", "c@example.com");

        var message = await MimeMessage.LoadAsync(new MemoryStream(saved!));
        message.Subject.Should().Be("Ünicode");
        message.HtmlBody.Should().Contain("<strong>x</strong>");
    }

    [TestCase(null, true)]
    [TestCase(true, true)]
    [TestCase(false, false)]
    public async Task SaveDraftAsync_PassesNotifyToTheHandler_DefaultingToTrue(bool? notify, bool expected)
    {
        var handler = new Mock<IReceivedDataHandler>();
        handler.Setup(h => h.HandleReceivedAsync(It.IsAny<byte[]>(), It.IsAny<string[]>(), It.IsAny<bool>())).ReturnsAsync("file.eml");

        var draft = new MessageDraft { Subject = "s" };
        if (notify is { } value) draft.Notify = value;

        await handler.Object.SaveDraftAsync(draft);

        handler.Verify(h => h.HandleReceivedAsync(It.IsAny<byte[]>(), It.IsAny<string[]>(), expected), Times.Once);
    }
}
