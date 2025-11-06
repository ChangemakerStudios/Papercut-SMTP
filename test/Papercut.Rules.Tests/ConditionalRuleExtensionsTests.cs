// Papercut
//
// Copyright © 2008 - 2012 Ken Robertson
// Copyright © 2013 - 2024 Jaben Cargman
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
using Papercut.Rules.App.Conditional;
using Papercut.Rules.Domain.Conditional;

namespace Papercut.Rules.Tests;

[TestFixture]
public class ConditionalRuleExtensionsTests
{
    private MimeMessage CreateTestMessage(string subject = "Test", string body = "Test body", string? headerName = null, string? headerValue = null)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress("Sender", "sender@test.com"));
        message.To.Add(new MailboxAddress("Recipient", "recipient@test.com"));
        message.Subject = subject;

        if (headerName != null && headerValue != null)
        {
            message.Headers.Add(headerName, headerValue);
        }

        message.Body = new TextPart("plain") { Text = body };

        return message;
    }

    #region Null Checks

    [Test]
    public void IsConditionalForwardRuleMatch_WithNullRule_ThrowsArgumentNullException()
    {
        var message = CreateTestMessage();
        var action = () => ConditionalRuleExtensions.IsConditionalForwardRuleMatch(null!, message);

        action.Should().Throw<ArgumentNullException>();
    }

    [Test]
    public void IsConditionalForwardRuleMatch_WithNullMessage_ThrowsArgumentNullException()
    {
        var mockRule = new Mock<IConditionalRule>();
        var action = () => mockRule.Object.IsConditionalForwardRuleMatch(null!);

        action.Should().Throw<ArgumentNullException>();
    }

    #endregion

    #region No Conditions Tests

    [Test]
    public void IsConditionalForwardRuleMatch_WithNoConditions_ReturnsTrue()
    {
        var mockRule = new Mock<IConditionalRule>();
        mockRule.Setup(r => r.RegexHeaderMatch).Returns(string.Empty);
        mockRule.Setup(r => r.RegexBodyMatch).Returns(string.Empty);

        var message = CreateTestMessage();

        var result = mockRule.Object.IsConditionalForwardRuleMatch(message);

        result.Should().BeTrue();
    }

    [Test]
    public void IsConditionalForwardRuleMatch_WithNullConditions_ReturnsTrue()
    {
        var mockRule = new Mock<IConditionalRule>();
        mockRule.Setup(r => r.RegexHeaderMatch).Returns((string?)null);
        mockRule.Setup(r => r.RegexBodyMatch).Returns((string?)null);

        var message = CreateTestMessage();

        var result = mockRule.Object.IsConditionalForwardRuleMatch(message);

        result.Should().BeTrue();
    }

    #endregion

    #region Header Matching Tests

    [Test]
    public void IsConditionalForwardRuleMatch_WithMatchingHeaderPattern_ReturnsTrue()
    {
        var mockRule = new Mock<IConditionalRule>();
        mockRule.Setup(r => r.RegexHeaderMatch).Returns("Subject:.*Test");
        mockRule.Setup(r => r.RegexBodyMatch).Returns(string.Empty);

        var message = CreateTestMessage(subject: "Test Subject");

        var result = mockRule.Object.IsConditionalForwardRuleMatch(message);

        result.Should().BeTrue();
    }

    [Test]
    public void IsConditionalForwardRuleMatch_WithNonMatchingHeaderPattern_ReturnsFalse()
    {
        var mockRule = new Mock<IConditionalRule>();
        mockRule.Setup(r => r.RegexHeaderMatch).Returns("Subject:.*Important");
        mockRule.Setup(r => r.RegexBodyMatch).Returns(string.Empty);

        var message = CreateTestMessage(subject: "Test Subject");

        var result = mockRule.Object.IsConditionalForwardRuleMatch(message);

        result.Should().BeFalse();
    }

    [Test]
    public void IsConditionalForwardRuleMatch_WithCaseInsensitiveHeaderMatch_ReturnsTrue()
    {
        var mockRule = new Mock<IConditionalRule>();
        mockRule.Setup(r => r.RegexHeaderMatch).Returns("subject:.*test");
        mockRule.Setup(r => r.RegexBodyMatch).Returns(string.Empty);

        var message = CreateTestMessage(subject: "TEST SUBJECT");

        var result = mockRule.Object.IsConditionalForwardRuleMatch(message);

        result.Should().BeTrue();
    }

    [Test]
    public void IsConditionalForwardRuleMatch_WithCustomHeader_MatchesCorrectly()
    {
        var mockRule = new Mock<IConditionalRule>();
        mockRule.Setup(r => r.RegexHeaderMatch).Returns("X-Custom-Header:.*special");
        mockRule.Setup(r => r.RegexBodyMatch).Returns(string.Empty);

        var message = CreateTestMessage(headerName: "X-Custom-Header", headerValue: "special-value");

        var result = mockRule.Object.IsConditionalForwardRuleMatch(message);

        result.Should().BeTrue();
    }

    [Test]
    public void IsConditionalForwardRuleMatch_WithMultilineHeaders_MatchesAcrossLines()
    {
        var mockRule = new Mock<IConditionalRule>();
        // Use [\s\S]* instead of .* to match across newlines
        mockRule.Setup(r => r.RegexHeaderMatch).Returns("From:[\\s\\S]*sender@test\\.com[\\s\\S]*To:[\\s\\S]*recipient");
        mockRule.Setup(r => r.RegexBodyMatch).Returns(string.Empty);

        var message = CreateTestMessage();

        var result = mockRule.Object.IsConditionalForwardRuleMatch(message);

        result.Should().BeTrue();
    }

    [Test]
    public void IsConditionalForwardRuleMatch_WithRegexSpecialChars_HandlesCorrectly()
    {
        var mockRule = new Mock<IConditionalRule>();
        mockRule.Setup(r => r.RegexHeaderMatch).Returns(@"Subject:.*\[Important\]");
        mockRule.Setup(r => r.RegexBodyMatch).Returns(string.Empty);

        var message = CreateTestMessage(subject: "[Important] Test");

        var result = mockRule.Object.IsConditionalForwardRuleMatch(message);

        result.Should().BeTrue();
    }

    #endregion

    #region Body Matching Tests

    [Test]
    public void IsConditionalForwardRuleMatch_WithMatchingBodyPattern_ReturnsTrue()
    {
        var mockRule = new Mock<IConditionalRule>();
        mockRule.Setup(r => r.RegexHeaderMatch).Returns(string.Empty);
        mockRule.Setup(r => r.RegexBodyMatch).Returns("urgent.*action");

        var message = CreateTestMessage(body: "This is urgent and requires action");

        var result = mockRule.Object.IsConditionalForwardRuleMatch(message);

        result.Should().BeTrue();
    }

    [Test]
    public void IsConditionalForwardRuleMatch_WithNonMatchingBodyPattern_ReturnsFalse()
    {
        var mockRule = new Mock<IConditionalRule>();
        mockRule.Setup(r => r.RegexHeaderMatch).Returns(string.Empty);
        mockRule.Setup(r => r.RegexBodyMatch).Returns("urgent.*action");

        var message = CreateTestMessage(body: "This is a normal message");

        var result = mockRule.Object.IsConditionalForwardRuleMatch(message);

        result.Should().BeFalse();
    }

    [Test]
    public void IsConditionalForwardRuleMatch_WithCaseInsensitiveBodyMatch_ReturnsTrue()
    {
        var mockRule = new Mock<IConditionalRule>();
        mockRule.Setup(r => r.RegexHeaderMatch).Returns(string.Empty);
        mockRule.Setup(r => r.RegexBodyMatch).Returns("urgent");

        var message = CreateTestMessage(body: "THIS IS URGENT");

        var result = mockRule.Object.IsConditionalForwardRuleMatch(message);

        result.Should().BeTrue();
    }

    [Test]
    public void IsConditionalForwardRuleMatch_WithEmptyBody_MatchesEmptyPattern()
    {
        var mockRule = new Mock<IConditionalRule>();
        mockRule.Setup(r => r.RegexHeaderMatch).Returns(string.Empty);
        // The body text extraction joins text parts, so empty message has no parts, resulting in empty string
        mockRule.Setup(r => r.RegexBodyMatch).Returns("^$");

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress("Sender", "sender@test.com"));
        message.To.Add(new MailboxAddress("Recipient", "recipient@test.com"));
        message.Subject = "Test";
        // Don't set Body at all - this makes it truly empty

        var result = mockRule.Object.IsConditionalForwardRuleMatch(message);

        result.Should().BeTrue();
    }

    [Test]
    public void IsConditionalForwardRuleMatch_IgnoresAttachments_OnlyMatchesBodyText()
    {
        var mockRule = new Mock<IConditionalRule>();
        mockRule.Setup(r => r.RegexHeaderMatch).Returns(string.Empty);
        mockRule.Setup(r => r.RegexBodyMatch).Returns("body text");

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress("Sender", "sender@test.com"));
        message.To.Add(new MailboxAddress("Recipient", "recipient@test.com"));
        message.Subject = "Test";

        var multipart = new Multipart("mixed");
        multipart.Add(new TextPart("plain") { Text = "This is body text" });
        multipart.Add(new TextPart("plain") { Text = "Attachment content", ContentDisposition = new ContentDisposition("attachment") });
        message.Body = multipart;

        var result = mockRule.Object.IsConditionalForwardRuleMatch(message);

        result.Should().BeTrue();
    }

    #endregion

    #region Combined Header and Body Tests

    [Test]
    public void IsConditionalForwardRuleMatch_WithBothHeaderAndBodyMatching_ReturnsTrue()
    {
        var mockRule = new Mock<IConditionalRule>();
        mockRule.Setup(r => r.RegexHeaderMatch).Returns("Subject:.*Important");
        mockRule.Setup(r => r.RegexBodyMatch).Returns("urgent");

        var message = CreateTestMessage(subject: "Important Notice", body: "This is urgent");

        var result = mockRule.Object.IsConditionalForwardRuleMatch(message);

        result.Should().BeTrue();
    }

    [Test]
    public void IsConditionalForwardRuleMatch_WithHeaderMatchingButBodyNotMatching_ReturnsFalse()
    {
        var mockRule = new Mock<IConditionalRule>();
        mockRule.Setup(r => r.RegexHeaderMatch).Returns("Subject:.*Important");
        mockRule.Setup(r => r.RegexBodyMatch).Returns("urgent");

        var message = CreateTestMessage(subject: "Important Notice", body: "Normal message");

        var result = mockRule.Object.IsConditionalForwardRuleMatch(message);

        result.Should().BeFalse();
    }

    [Test]
    public void IsConditionalForwardRuleMatch_WithBodyMatchingButHeaderNotMatching_ReturnsFalse()
    {
        var mockRule = new Mock<IConditionalRule>();
        mockRule.Setup(r => r.RegexHeaderMatch).Returns("Subject:.*Important");
        mockRule.Setup(r => r.RegexBodyMatch).Returns("urgent");

        var message = CreateTestMessage(subject: "Normal Subject", body: "This is urgent");

        var result = mockRule.Object.IsConditionalForwardRuleMatch(message);

        result.Should().BeFalse();
    }

    [Test]
    public void IsConditionalForwardRuleMatch_WithBothNotMatching_ReturnsFalse()
    {
        var mockRule = new Mock<IConditionalRule>();
        mockRule.Setup(r => r.RegexHeaderMatch).Returns("Subject:.*Important");
        mockRule.Setup(r => r.RegexBodyMatch).Returns("urgent");

        var message = CreateTestMessage(subject: "Normal Subject", body: "Normal message");

        var result = mockRule.Object.IsConditionalForwardRuleMatch(message);

        result.Should().BeFalse();
    }

    #endregion

    #region Edge Cases

    [Test]
    public void IsConditionalForwardRuleMatch_WithInvalidRegex_ThrowsException()
    {
        var mockRule = new Mock<IConditionalRule>();
        mockRule.Setup(r => r.RegexHeaderMatch).Returns("[invalid regex");
        mockRule.Setup(r => r.RegexBodyMatch).Returns(string.Empty);

        var message = CreateTestMessage();
        var action = () => mockRule.Object.IsConditionalForwardRuleMatch(message);

        action.Should().Throw<ArgumentException>();
    }

    [Test]
    public void IsConditionalForwardRuleMatch_WithComplexRegex_MatchesCorrectly()
    {
        var mockRule = new Mock<IConditionalRule>();
        mockRule.Setup(r => r.RegexHeaderMatch).Returns(@"Subject:.*\b\d{3,}\b");
        mockRule.Setup(r => r.RegexBodyMatch).Returns(string.Empty);

        var message = CreateTestMessage(subject: "Order 12345 Confirmation");

        var result = mockRule.Object.IsConditionalForwardRuleMatch(message);

        result.Should().BeTrue();
    }

    [Test]
    public void IsConditionalForwardRuleMatch_WithWildcardPattern_MatchesAnything()
    {
        var mockRule = new Mock<IConditionalRule>();
        mockRule.Setup(r => r.RegexHeaderMatch).Returns(".*");
        mockRule.Setup(r => r.RegexBodyMatch).Returns(string.Empty);

        var message = CreateTestMessage();

        var result = mockRule.Object.IsConditionalForwardRuleMatch(message);

        result.Should().BeTrue();
    }

    #endregion
}
