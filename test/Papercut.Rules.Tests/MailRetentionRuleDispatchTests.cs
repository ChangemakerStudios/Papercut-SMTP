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
using Moq;
using NUnit.Framework;
using Papercut.Core.Domain.Message;
using Papercut.Message;
using Papercut.Rules.App.Cleanup;
using Papercut.Rules.Domain.Cleanup;
using Serilog;

namespace Papercut.Rules.Tests;

[TestFixture]
public class MailRetentionRuleDispatchTests
{
    private Mock<IMessageRepository> _mockRepository = null!;
    private Mock<ILogger> _mockLogger = null!;
    private MailRetentionRuleDispatch _dispatch = null!;

    [SetUp]
    public void SetUp()
    {
        _mockRepository = new Mock<IMessageRepository>();
        _mockLogger = new Mock<ILogger>();
        _dispatch = new MailRetentionRuleDispatch(_mockRepository.Object, _mockLogger.Object);
    }

    #region Constructor Tests

    [Test]
    public void Constructor_WithValidParameters_CreatesInstance()
    {
        var action = () => new MailRetentionRuleDispatch(_mockRepository.Object, _mockLogger.Object);
        action.Should().NotThrow();
    }

    [Test]
    public void Constructor_WithNullRepository_ThrowsArgumentNullException()
    {
        var action = () => new MailRetentionRuleDispatch(null!, _mockLogger.Object);
        action.Should().Throw<ArgumentNullException>();
    }

    [Test]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        var action = () => new MailRetentionRuleDispatch(_mockRepository.Object, null!);
        action.Should().Throw<ArgumentNullException>();
    }

    #endregion

    #region Parameter Validation Tests

    [Test]
    public async Task DispatchAsync_WithNullRule_ThrowsArgumentNullException()
    {
        var action = async () => await _dispatch.DispatchAsync(null!, null, CancellationToken.None);
        await action.Should().ThrowAsync<ArgumentNullException>();
    }

    [Test]
    public async Task DispatchAsync_WithZeroRetentionDays_LogsWarningAndReturns()
    {
        var rule = new MailRetentionRule { MailRetentionDays = 0, IsEnabled = true };

        await _dispatch.DispatchAsync(rule, null, CancellationToken.None);

        _mockLogger.Verify(
            l => l.Warning(It.Is<string>(s => s.Contains("invalid retention days")), It.IsAny<int>()),
            Times.Once);
        _mockRepository.Verify(r => r.LoadMessages(), Times.Never);
    }

    [Test]
    public async Task DispatchAsync_WithNegativeRetentionDays_LogsWarningAndReturns()
    {
        var rule = new MailRetentionRule { MailRetentionDays = -5, IsEnabled = true };

        await _dispatch.DispatchAsync(rule, null, CancellationToken.None);

        _mockLogger.Verify(
            l => l.Warning(It.Is<string>(s => s.Contains("invalid retention days")), It.IsAny<int>()),
            Times.Once);
        _mockRepository.Verify(r => r.LoadMessages(), Times.Never);
    }

    #endregion

    #region Message Deletion Tests

    [Test]
    public async Task DispatchAsync_WithNoMessages_CompletesWithoutDeletion()
    {
        var rule = new MailRetentionRule { MailRetentionDays = 7, IsEnabled = true };
        _mockRepository.Setup(r => r.LoadMessages()).Returns(Enumerable.Empty<MessageEntry>());

        await _dispatch.DispatchAsync(rule, null, CancellationToken.None);

        _mockRepository.Verify(r => r.DeleteMessage(It.IsAny<MessageEntry>()), Times.Never);
        _mockLogger.Verify(
            l => l.Debug(It.Is<string>(s => s.Contains("No messages found")), It.IsAny<DateTime>()),
            Times.Once);
    }

    [Test]
    public async Task DispatchAsync_WithRecentMessages_DoesNotDelete()
    {
        var rule = new MailRetentionRule { MailRetentionDays = 7, IsEnabled = true };
        var recentMessages = CreateMessageEntries(new[]
        {
            DateTime.Now.AddDays(-1),
            DateTime.Now.AddDays(-3),
            DateTime.Now.AddDays(-6)
        });

        _mockRepository.Setup(r => r.LoadMessages()).Returns(recentMessages);

        await _dispatch.DispatchAsync(rule, null, CancellationToken.None);

        _mockRepository.Verify(r => r.DeleteMessage(It.IsAny<MessageEntry>()), Times.Never);
    }

    [Test]
    public async Task DispatchAsync_WithOldMessages_DeletesThem()
    {
        var rule = new MailRetentionRule { MailRetentionDays = 7, IsEnabled = true };
        var oldMessages = CreateMessageEntries(new[]
        {
            DateTime.Now.AddDays(-10),
            DateTime.Now.AddDays(-15),
            DateTime.Now.AddDays(-30)
        });

        _mockRepository.Setup(r => r.LoadMessages()).Returns(oldMessages);
        _mockRepository.Setup(r => r.DeleteMessage(It.IsAny<MessageEntry>())).Returns(true);

        await _dispatch.DispatchAsync(rule, null, CancellationToken.None);

        _mockRepository.Verify(r => r.DeleteMessage(It.IsAny<MessageEntry>()), Times.Exactly(3));
        _mockLogger.Verify(
            l => l.Information(
                It.Is<string>(s => s.Contains("Found") && s.Contains("messages to delete")),
                It.IsAny<int>(),
                It.IsAny<DateTime>()),
            Times.Once);
    }

    [Test]
    public async Task DispatchAsync_WithMixedMessages_DeletesOnlyOldOnes()
    {
        var rule = new MailRetentionRule { MailRetentionDays = 7, IsEnabled = true };
        var mixedMessages = CreateMessageEntries(new[]
        {
            DateTime.Now.AddDays(-10), // old - should delete
            DateTime.Now.AddDays(-5),  // recent - keep
            DateTime.Now.AddDays(-15), // old - should delete
            DateTime.Now.AddDays(-2),  // recent - keep
            DateTime.Now.AddDays(-30)  // old - should delete
        });

        _mockRepository.Setup(r => r.LoadMessages()).Returns(mixedMessages);
        _mockRepository.Setup(r => r.DeleteMessage(It.IsAny<MessageEntry>())).Returns(true);

        await _dispatch.DispatchAsync(rule, null, CancellationToken.None);

        _mockRepository.Verify(r => r.DeleteMessage(It.IsAny<MessageEntry>()), Times.Exactly(3));
    }

    [Test]
    public async Task DispatchAsync_WithExactCutoffDate_DoesNotDelete()
    {
        var rule = new MailRetentionRule { MailRetentionDays = 7, IsEnabled = true };
        // Create a message that is slightly newer than 7 days to ensure it's not deleted
        var exactCutoffMessages = CreateMessageEntries(new[]
        {
            DateTime.Now.AddDays(-7).AddMinutes(1) // Just under 7 days - should NOT delete
        });

        _mockRepository.Setup(r => r.LoadMessages()).Returns(exactCutoffMessages);

        await _dispatch.DispatchAsync(rule, null, CancellationToken.None);

        _mockRepository.Verify(r => r.DeleteMessage(It.IsAny<MessageEntry>()), Times.Never);
    }

    #endregion

    #region Error Handling Tests

    [Test]
    public async Task DispatchAsync_WithDeletionFailure_LogsErrorAndContinues()
    {
        var rule = new MailRetentionRule { MailRetentionDays = 7, IsEnabled = true };
        var oldMessages = CreateMessageEntries(new[]
        {
            DateTime.Now.AddDays(-10),
            DateTime.Now.AddDays(-15)
        });

        _mockRepository.Setup(r => r.LoadMessages()).Returns(oldMessages);
        _mockRepository.Setup(r => r.DeleteMessage(It.IsAny<MessageEntry>()))
            .Throws(new IOException("File locked"));

        await _dispatch.DispatchAsync(rule, null, CancellationToken.None);

        _mockLogger.Verify(
            l => l.Error(
                It.IsAny<IOException>(),
                It.Is<string>(s => s.Contains("Error deleting message")),
                It.IsAny<string>()),
            Times.Exactly(2));
    }

    [Test]
    public async Task DispatchAsync_WithPartialDeletionFailure_ReportsCorrectCounts()
    {
        var rule = new MailRetentionRule { MailRetentionDays = 7, IsEnabled = true };
        var oldMessages = CreateMessageEntries(new[]
        {
            DateTime.Now.AddDays(-10),
            DateTime.Now.AddDays(-15),
            DateTime.Now.AddDays(-20)
        }).ToList();

        _mockRepository.Setup(r => r.LoadMessages()).Returns(oldMessages);

        // First succeeds, second fails, third succeeds
        var callCount = 0;
        _mockRepository.Setup(r => r.DeleteMessage(It.IsAny<MessageEntry>()))
            .Returns(() =>
            {
                callCount++;
                if (callCount == 2)
                    throw new UnauthorizedAccessException("Access denied");
                return true;
            });

        await _dispatch.DispatchAsync(rule, null, CancellationToken.None);

        _mockLogger.Verify(
            l => l.Information(
                It.Is<string>(s => s.Contains("Mail cleanup completed")),
                It.Is<int>(count => count == 2), // 2 deleted
                It.Is<int>(count => count == 1)), // 1 failed
            Times.Once);
    }

    [Test]
    public async Task DispatchAsync_WithFileNotFound_LogsWarningAndContinues()
    {
        var rule = new MailRetentionRule { MailRetentionDays = 7, IsEnabled = true };
        var oldMessages = CreateMessageEntries(new[]
        {
            DateTime.Now.AddDays(-10)
        });

        _mockRepository.Setup(r => r.LoadMessages()).Returns(oldMessages);
        _mockRepository.Setup(r => r.DeleteMessage(It.IsAny<MessageEntry>())).Returns(false); // File not found

        await _dispatch.DispatchAsync(rule, null, CancellationToken.None);

        _mockLogger.Verify(
            l => l.Warning(
                It.Is<string>(s => s.Contains("Failed to delete message") && s.Contains("file not found")),
                It.IsAny<string>()),
            Times.Once);
    }

    #endregion

    #region Cancellation Tests

    [Test]
    public async Task DispatchAsync_WithCancelledToken_ThrowsOperationCanceledException()
    {
        var rule = new MailRetentionRule { MailRetentionDays = 7, IsEnabled = true };
        var oldMessages = CreateMessageEntries(new[]
        {
            DateTime.Now.AddDays(-10),
            DateTime.Now.AddDays(-15)
        });

        _mockRepository.Setup(r => r.LoadMessages()).Returns(oldMessages);

        var cts = new CancellationTokenSource();
        cts.Cancel();

        var action = async () => await _dispatch.DispatchAsync(rule, null, cts.Token);
        await action.Should().ThrowAsync<OperationCanceledException>();
    }

    [Test]
    public async Task DispatchAsync_WithTokenCancelledDuringDeletion_StopsProcessing()
    {
        var rule = new MailRetentionRule { MailRetentionDays = 7, IsEnabled = true };
        var oldMessages = CreateMessageEntries(new[]
        {
            DateTime.Now.AddDays(-10),
            DateTime.Now.AddDays(-15),
            DateTime.Now.AddDays(-20)
        });

        _mockRepository.Setup(r => r.LoadMessages()).Returns(oldMessages);

        var cts = new CancellationTokenSource();
        var deleteCount = 0;
        _mockRepository.Setup(r => r.DeleteMessage(It.IsAny<MessageEntry>()))
            .Returns(() =>
            {
                deleteCount++;
                if (deleteCount == 2)
                    cts.Cancel();
                return true;
            });

        var action = async () => await _dispatch.DispatchAsync(rule, null, cts.Token);
        await action.Should().ThrowAsync<OperationCanceledException>();

        // Should have processed at least 2 deletions before cancellation
        _mockRepository.Verify(r => r.DeleteMessage(It.IsAny<MessageEntry>()), Times.AtLeast(2));
    }

    #endregion

    #region Logging Tests

    [Test]
    public async Task DispatchAsync_WithSuccessfulDeletion_LogsAllStages()
    {
        var rule = new MailRetentionRule { MailRetentionDays = 7, IsEnabled = true };
        var oldMessages = CreateMessageEntries(new[] { DateTime.Now.AddDays(-10) });

        _mockRepository.Setup(r => r.LoadMessages()).Returns(oldMessages);
        _mockRepository.Setup(r => r.DeleteMessage(It.IsAny<MessageEntry>())).Returns(true);

        await _dispatch.DispatchAsync(rule, null, CancellationToken.None);

        // Debug: Starting cleanup
        _mockLogger.Verify(
            l => l.Debug(It.Is<string>(s => s.Contains("Starting mail cleanup")), It.IsAny<DateTime>(), It.IsAny<int>()),
            Times.Once);

        // Information: Found messages to delete
        _mockLogger.Verify(
            l => l.Information(It.Is<string>(s => s.Contains("Found") && s.Contains("messages to delete")), It.IsAny<int>(), It.IsAny<DateTime>()),
            Times.Once);

        // Debug: Individual file deletion
        _mockLogger.Verify(
            l => l.Debug(It.Is<string>(s => s.Contains("Deleted message")), It.IsAny<string>()),
            Times.Once);

        // Information: Cleanup completed
        _mockLogger.Verify(
            l => l.Information(It.Is<string>(s => s.Contains("Mail cleanup completed")), It.IsAny<int>(), It.IsAny<int>()),
            Times.Once);
    }

    #endregion

    #region Different Retention Period Tests

    [Test]
    [TestCase(1)]
    [TestCase(7)]
    [TestCase(30)]
    [TestCase(365)]
    public async Task DispatchAsync_WithDifferentRetentionPeriods_CalculatesCorrectCutoff(int retentionDays)
    {
        var rule = new MailRetentionRule { MailRetentionDays = retentionDays, IsEnabled = true };
        var messages = CreateMessageEntries(new[]
        {
            DateTime.Now.AddDays(-(retentionDays - 1)), // Should keep
            DateTime.Now.AddDays(-(retentionDays + 1))  // Should delete
        });

        _mockRepository.Setup(r => r.LoadMessages()).Returns(messages);
        _mockRepository.Setup(r => r.DeleteMessage(It.IsAny<MessageEntry>())).Returns(true);

        await _dispatch.DispatchAsync(rule, null, CancellationToken.None);

        // Should delete exactly 1 message (the one older than retention period)
        _mockRepository.Verify(r => r.DeleteMessage(It.IsAny<MessageEntry>()), Times.Once);
    }

    #endregion

    #region Helper Methods

    private static IEnumerable<MessageEntry> CreateMessageEntries(DateTime[] dates)
    {
        foreach (var date in dates)
        {
            var tempFile = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.eml");
            File.WriteAllText(tempFile, "test email content");
            File.SetLastWriteTime(tempFile, date);

            yield return new MessageEntry(tempFile);
        }
    }

    [TearDown]
    public void TearDown()
    {
        // Cleanup any temporary files created during tests
        var tempPath = Path.GetTempPath();
        foreach (var file in Directory.GetFiles(tempPath, "test_*.eml"))
        {
            try
            {
                File.Delete(file);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }

    #endregion
}
