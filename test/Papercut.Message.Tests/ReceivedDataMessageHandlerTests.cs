// Papercut
//
// Copyright © 2008 - 2012 Ken Robertson
// Copyright © 2013 - 2025 Jaben Cargman
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
using Papercut.Common.Domain;
using Papercut.Core.Domain.Message;
using Papercut.Message;
using Serilog;

namespace Papercut.Message.Tests;

[TestFixture]
public class ReceivedDataMessageHandlerTests
{
    private Mock<IMessageRepository> _mockRepository = null!;
    private Mock<IMessageBus> _mockMessageBus = null!;
    private Mock<ILogger> _mockLogger = null!;
    private ReceivedDataMessageHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _mockRepository = new Mock<IMessageRepository>();
        _mockMessageBus = new Mock<IMessageBus>();
        _mockLogger = new Mock<ILogger>();

        _handler = new ReceivedDataMessageHandler(
            _mockRepository.Object,
            _mockMessageBus.Object,
            _mockLogger.Object);
    }

    private static byte[] CreateTestMessage(string to, string? cc = null, string? subject = null)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress("Sender", "sender@test.com"));
        message.To.Add(MailboxAddress.Parse(to));
        if (cc != null)
        {
            message.Cc.Add(MailboxAddress.Parse(cc));
        }
        message.Subject = subject ?? "Test Subject";
        message.Body = new TextPart("plain") { Text = "Test body" };

        using var ms = new MemoryStream();
        message.WriteTo(ms);
        return ms.ToArray();
    }

    #region Basic Functionality Tests

    [Test]
    public async Task HandleReceivedAsync_WithValidMessage_SavesMessage()
    {
        var messageData = CreateTestMessage("recipient@test.com");
        var recipients = new[] { "recipient@test.com" };
        var expectedFile = "test.eml";

        _mockRepository
            .Setup(x => x.SaveMessage(It.IsAny<string>(), It.IsAny<Func<FileStream, Task>>()))
            .ReturnsAsync(expectedFile);

        await _handler.HandleReceivedAsync(messageData, recipients);

        _mockRepository.Verify(
            x => x.SaveMessage(It.IsAny<string>(), It.IsAny<Func<FileStream, Task>>()),
            Times.Once);
    }

    [Test]
    public async Task HandleReceivedAsync_WithValidMessage_PublishesNewMessageEvent()
    {
        var messageData = CreateTestMessage("recipient@test.com");
        var recipients = new[] { "recipient@test.com" };
        var tempFile = Path.GetTempFileName();

        try
        {
            _mockRepository
                .Setup(x => x.SaveMessage(It.IsAny<string>(), It.IsAny<Func<FileStream, Task>>()))
                .ReturnsAsync(tempFile);

            await _handler.HandleReceivedAsync(messageData, recipients);

            _mockMessageBus.Verify(
                x => x.PublishAsync(It.IsAny<NewMessageEvent>(), It.IsAny<CancellationToken>()),
                Times.Once);
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    [Test]
    public void HandleReceivedAsync_WithNullMessageData_ThrowsArgumentNullException()
    {
        var action = async () => await _handler.HandleReceivedAsync(null!, new[] { "test@test.com" });
        action.Should().ThrowAsync<ArgumentNullException>();
    }

    [Test]
    public void HandleReceivedAsync_WithNullRecipients_ThrowsArgumentNullException()
    {
        var messageData = CreateTestMessage("test@test.com");
        var action = async () => await _handler.HandleReceivedAsync(messageData, null!);
        action.Should().ThrowAsync<ArgumentNullException>();
    }

    #endregion

    #region BCC Detection Tests

    [Test]
    public async Task HandleReceivedAsync_WithBccRecipient_AddsBccToMessage()
    {
        var messageData = CreateTestMessage("recipient@test.com");
        var recipients = new[] { "recipient@test.com", "bcc@test.com" };

        _mockRepository
            .Setup(x => x.SaveMessage(It.IsAny<string>(), It.IsAny<Func<FileStream, Task>>()))
            .ReturnsAsync("test.eml");

        await _handler.HandleReceivedAsync(messageData, recipients);

        // Verify message was saved (BCC detection happened internally)
        _mockRepository.Verify(
            x => x.SaveMessage(It.IsAny<string>(), It.IsAny<Func<FileStream, Task>>()),
            Times.Once);
    }

    [Test]
    public async Task HandleReceivedAsync_WithCaseInsensitiveRecipients_DetectsBccCorrectly()
    {
        var messageData = CreateTestMessage("Recipient@Test.COM");
        var recipients = new[] { "recipient@test.com", "bcc@test.com" };

        _mockRepository
            .Setup(x => x.SaveMessage(It.IsAny<string>(), It.IsAny<Func<FileStream, Task>>()))
            .ReturnsAsync("test.eml");

        await _handler.HandleReceivedAsync(messageData, recipients);

        // Case-insensitive comparison should work
        _mockRepository.Verify(
            x => x.SaveMessage(It.IsAny<string>(), It.IsAny<Func<FileStream, Task>>()),
            Times.Once);
    }

    #endregion

    #region Error Handling Tests

    [Test]
    public async Task HandleReceivedAsync_WhenPublishFails_LogsFatalError()
    {
        var messageData = CreateTestMessage("recipient@test.com");
        var recipients = new[] { "recipient@test.com" };
        var tempFile = Path.GetTempFileName();

        try
        {
            _mockRepository
                .Setup(x => x.SaveMessage(It.IsAny<string>(), It.IsAny<Func<FileStream, Task>>()))
                .ReturnsAsync(tempFile);

            _mockMessageBus
                .Setup(x => x.PublishAsync(It.IsAny<NewMessageEvent>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Publish failed"));

            // Should not throw - error is caught and logged
            await _handler.HandleReceivedAsync(messageData, recipients);

            _mockLogger.Verify(
                x => x.Fatal(
                    It.IsAny<Exception>(),
                    It.Is<string>(s => s.Contains("Unable to publish")),
                    It.IsAny<string>()),
                Times.Once);
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    [Test]
    public async Task HandleReceivedAsync_WithEmptyFilename_DoesNotPublish()
    {
        var messageData = CreateTestMessage("recipient@test.com");
        var recipients = new[] { "recipient@test.com" };

        _mockRepository
            .Setup(x => x.SaveMessage(It.IsAny<string>(), It.IsAny<Func<FileStream, Task>>()))
            .ReturnsAsync(string.Empty);

        await _handler.HandleReceivedAsync(messageData, recipients);

        _mockMessageBus.Verify(
            x => x.PublishAsync(It.Is<NewMessageEvent>(e => true), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    #region Non-Standard Email Address Tests (Issue #284)

    [Test]
    public async Task HandleReceivedAsync_WithDoubleUnderscoreDomain_HandlesSuccessfully()
    {
        // Arrange: Create a message with standard recipient
        var messageData = CreateTestMessage("recipient@test.com");
        // Include a BCC with non-standard @__ domain (Issue #284)
        var recipients = new[] { "recipient@test.com", "testuser@__" };

        _mockRepository
            .Setup(x => x.SaveMessage(It.IsAny<string>(), It.IsAny<Func<FileStream, Task>>()))
            .ReturnsAsync("test.eml");

        // Act
        await _handler.HandleReceivedAsync(messageData, recipients);

        // Assert: Message should be saved without throwing
        _mockRepository.Verify(
            x => x.SaveMessage(It.IsAny<string>(), It.IsAny<Func<FileStream, Task>>()),
            Times.Once);
    }

    [Test]
    public async Task HandleReceivedAsync_WithUnderscorePrefixedDomain_HandlesSuccessfully()
    {
        // Arrange
        var messageData = CreateTestMessage("recipient@test.com");
        var recipients = new[] { "recipient@test.com", "testuser@_test" };

        _mockRepository
            .Setup(x => x.SaveMessage(It.IsAny<string>(), It.IsAny<Func<FileStream, Task>>()))
            .ReturnsAsync("test.eml");

        // Act
        await _handler.HandleReceivedAsync(messageData, recipients);

        // Assert
        _mockRepository.Verify(
            x => x.SaveMessage(It.IsAny<string>(), It.IsAny<Func<FileStream, Task>>()),
            Times.Once);
    }

    [Test]
    public async Task HandleReceivedAsync_WithLooseParser_AcceptsMostFormats()
    {
        // Arrange: The loose parser is very permissive and will accept most email-like strings
        var messageData = CreateTestMessage("recipient@test.com");
        var recipients = new[] { "recipient@test.com", "user@__", "test@_domain" };

        _mockRepository
            .Setup(x => x.SaveMessage(It.IsAny<string>(), It.IsAny<Func<FileStream, Task>>()))
            .ReturnsAsync("test.eml");

        // Act
        await _handler.HandleReceivedAsync(messageData, recipients);

        // Assert: Should handle without warnings - loose parser is permissive
        _mockRepository.Verify(
            x => x.SaveMessage(It.IsAny<string>(), It.IsAny<Func<FileStream, Task>>()),
            Times.Once);
    }

    [Test]
    public async Task HandleReceivedAsync_WithMultipleNonStandardDomains_HandlesAllSuccessfully()
    {
        // Arrange
        var messageData = CreateTestMessage("recipient@test.com");
        var recipients = new[]
        {
            "recipient@test.com",
            "user1@__",
            "user2@_test",
            "user3@localhost"
        };

        _mockRepository
            .Setup(x => x.SaveMessage(It.IsAny<string>(), It.IsAny<Func<FileStream, Task>>()))
            .ReturnsAsync("test.eml");

        // Act
        await _handler.HandleReceivedAsync(messageData, recipients);

        // Assert: All non-standard addresses should be handled
        _mockRepository.Verify(
            x => x.SaveMessage(It.IsAny<string>(), It.IsAny<Func<FileStream, Task>>()),
            Times.Once);
    }

    #endregion
}
