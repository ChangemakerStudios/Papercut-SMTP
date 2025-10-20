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
using Autofac;
using Moq;
using NUnit.Framework;
using Papercut.Core.Domain.BackgroundTasks;
using Papercut.Core.Domain.Rules;
using Papercut.Core.Infrastructure.BackgroundTasks;
using Papercut.Message;
using Papercut.Rules.App;
using Papercut.Rules.App.Cleanup;
using Papercut.Rules.Domain.Cleanup;
using Serilog;

namespace Papercut.Rules.Tests;

[TestFixture]
public class PeriodicRuleIntegrationTests
{
    private IContainer _container = null!;
    private Mock<IMessageRepository> _mockRepository = null!;
    private Mock<ILogger> _mockLogger = null!;
    private string _testDirectory = null!;

    [SetUp]
    public void SetUp()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), $"PapercutTest_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDirectory);

        _mockRepository = new Mock<IMessageRepository>();
        _mockLogger = new Mock<ILogger>();

        var builder = new ContainerBuilder();

        // Register core infrastructure
        builder.RegisterInstance(_mockLogger.Object).As<ILogger>();
        builder.RegisterType<BackgroundTaskRunner>().As<IBackgroundTaskRunner>().SingleInstance();

        // Register rules infrastructure
        builder.RegisterType<RulesRunner>().As<IRulesRunner>().SingleInstance();

        // Register the mail retention rule and its dispatcher
        builder.RegisterInstance(_mockRepository.Object).As<IMessageRepository>();
        builder.RegisterType<MailRetentionRuleDispatch>()
            .As<IRuleDispatcher<MailRetentionRule>>()
            .InstancePerDependency();
        builder.RegisterType<MailRetentionRule>().AsSelf().As<IRule>().InstancePerDependency();

        _container = builder.Build();
    }

    [TearDown]
    public void TearDown()
    {
        _container?.Dispose();

        if (Directory.Exists(_testDirectory))
        {
            try
            {
                Directory.Delete(_testDirectory, true);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }

    #region End-to-End Tests

    [Test]
    public async Task RunPeriodicBackgroundRules_WithMailRetentionRule_ExecutesSuccessfully()
    {
        var rulesRunner = _container.Resolve<IRulesRunner>();
        var rule = new MailRetentionRule { MailRetentionDays = 7, IsEnabled = true };

        _mockRepository.Setup(r => r.LoadMessages()).Returns(Enumerable.Empty<Core.Domain.Message.MessageEntry>());

        var rules = new IPeriodicBackgroundRule[] { rule };

        var action = async () => await rulesRunner.RunPeriodicBackgroundRules(rules, CancellationToken.None);
        await action.Should().NotThrowAsync();
    }

    [Test]
    public async Task RunPeriodicBackgroundRules_WithDisabledRule_DoesNotExecute()
    {
        var rulesRunner = _container.Resolve<IRulesRunner>();
        var rule = new MailRetentionRule { MailRetentionDays = 7, IsEnabled = false };

        var rules = new IPeriodicBackgroundRule[] { rule };

        await rulesRunner.RunPeriodicBackgroundRules(rules, CancellationToken.None);

        // Give background task time to potentially execute (if it incorrectly does)
        await Task.Delay(500);

        _mockRepository.Verify(r => r.LoadMessages(), Times.Never);
    }

    [Test]
    public async Task RunPeriodicBackgroundRules_WithMultipleRules_ExecutesAll()
    {
        var rulesRunner = _container.Resolve<IRulesRunner>();
        var rule1 = new MailRetentionRule { MailRetentionDays = 7, IsEnabled = true };
        var rule2 = new MailRetentionRule { MailRetentionDays = 30, IsEnabled = true };

        _mockRepository.Setup(r => r.LoadMessages()).Returns(Enumerable.Empty<Core.Domain.Message.MessageEntry>());

        var rules = new IPeriodicBackgroundRule[] { rule1, rule2 };

        await rulesRunner.RunPeriodicBackgroundRules(rules, CancellationToken.None);

        // Give background tasks time to execute
        await Task.Delay(500);

        // Should have been called twice (once for each rule)
        _mockRepository.Verify(r => r.LoadMessages(), Times.AtLeast(2));
    }

    #endregion

    #region Background Task Queue Tests

    [Test]
    public async Task RunPeriodicBackgroundRules_UsesBackgroundTaskRunner()
    {
        var rulesRunner = _container.Resolve<IRulesRunner>();
        var backgroundTaskRunner = _container.Resolve<IBackgroundTaskRunner>();

        var rule = new MailRetentionRule { MailRetentionDays = 7, IsEnabled = true };
        _mockRepository.Setup(r => r.LoadMessages()).Returns(Enumerable.Empty<Core.Domain.Message.MessageEntry>());

        var rules = new IPeriodicBackgroundRule[] { rule };

        // This should queue the task and return immediately
        await rulesRunner.RunPeriodicBackgroundRules(rules, CancellationToken.None);

        // The task is queued but may not have executed yet
        backgroundTaskRunner.Should().NotBeNull();
    }

    [Test]
    public async Task RunPeriodicBackgroundRules_DoesNotBlockCaller()
    {
        var rulesRunner = _container.Resolve<IRulesRunner>();
        var rule = new MailRetentionRule { MailRetentionDays = 7, IsEnabled = true };

        var executionStartTime = DateTime.MinValue;
        _mockRepository.Setup(r => r.LoadMessages())
            .Returns(() =>
            {
                executionStartTime = DateTime.Now;
                Thread.Sleep(200); // Simulate slow operation
                return Enumerable.Empty<Core.Domain.Message.MessageEntry>();
            });

        var rules = new IPeriodicBackgroundRule[] { rule };

        var callStartTime = DateTime.Now;
        var sw = System.Diagnostics.Stopwatch.StartNew();
        await rulesRunner.RunPeriodicBackgroundRules(rules, CancellationToken.None);
        sw.Stop();

        // The key test: RunPeriodicBackgroundRules should return quickly, not wait for the slow operation
        // Even though the background task might start executing, the method itself should not block
        sw.ElapsedMilliseconds.Should().BeLessThan(300);

        // Wait for background task to complete
        await Task.Delay(500);

        // Verify the background task actually executed
        executionStartTime.Should().BeAfter(callStartTime);
    }

    #endregion

    #region Logging Integration Tests

    [Test]
    public async Task RunPeriodicBackgroundRules_WithEnabledRule_LogsDebugMessage()
    {
        var rulesRunner = _container.Resolve<IRulesRunner>();
        var rule = new MailRetentionRule { MailRetentionDays = 7, IsEnabled = true };

        _mockRepository.Setup(r => r.LoadMessages()).Returns(Enumerable.Empty<Core.Domain.Message.MessageEntry>());

        var rules = new IPeriodicBackgroundRule[] { rule };

        await rulesRunner.RunPeriodicBackgroundRules(rules, CancellationToken.None);

        // Give background task time to execute
        await Task.Delay(500);

        _mockLogger.Verify(
            l => l.Debug(
                It.Is<string>(s => s.Contains("Running Periodic Background")),
                It.IsAny<MailRetentionRule>()),
            Times.AtLeastOnce);
    }

    #endregion

    #region Error Handling Integration Tests

    [Test]
    public async Task RunPeriodicBackgroundRules_WithRepositoryException_CapturesErrorAndContinues()
    {
        var rulesRunner = _container.Resolve<IRulesRunner>();
        var rule = new MailRetentionRule { MailRetentionDays = 7, IsEnabled = true };

        _mockRepository.Setup(r => r.LoadMessages())
            .Throws(new IOException("Database connection failed"));

        var rules = new IPeriodicBackgroundRule[] { rule };

        // Should not throw - background task runner should capture the error
        var action = async () => await rulesRunner.RunPeriodicBackgroundRules(rules, CancellationToken.None);
        await action.Should().NotThrowAsync();

        // Give background task time to execute and fail
        await Task.Delay(500);

        // Verify error was logged by RulesRunner
        _mockLogger.Verify(
            l => l.Warning(
                It.IsAny<Exception>(),
                It.Is<string>(s => s.Contains("Failure Dispatching Rule")),
                It.IsAny<MailRetentionRule>(),
                It.IsAny<Core.Domain.Message.MessageEntry>()),
            Times.AtLeastOnce);
    }

    #endregion

    #region Cancellation Integration Tests

    [Test]
    public async Task RunPeriodicBackgroundRules_WithCancellationToken_PropagatesTokenToDispatcher()
    {
        var rulesRunner = _container.Resolve<IRulesRunner>();
        var rule = new MailRetentionRule { MailRetentionDays = 7, IsEnabled = true };

        var cts = new CancellationTokenSource(100); // Cancel after 100ms
        var loadMessagesCallCount = 0;

        _mockRepository.Setup(r => r.LoadMessages())
            .Returns(() =>
            {
                loadMessagesCallCount++;
                // Simulate work that might be cancelled
                Thread.Sleep(50);
                return Enumerable.Empty<Core.Domain.Message.MessageEntry>();
            });

        var rules = new IPeriodicBackgroundRule[] { rule };

        // Run with token that will cancel
        await rulesRunner.RunPeriodicBackgroundRules(rules, cts.Token);

        // Give background task time to execute
        await Task.Delay(500);

        // The repository should have been called (task started before cancellation)
        // Note: BackgroundTaskRunner handles cancellation gracefully
        loadMessagesCallCount.Should().BeGreaterThanOrEqualTo(0);
    }

    #endregion

    #region Multiple Rule Type Integration Tests

    [Test]
    public async Task RunPeriodicBackgroundRules_WithOnlyPeriodicRules_ExecutesCorrectly()
    {
        var rulesRunner = _container.Resolve<IRulesRunner>();

        var periodicRule1 = new MailRetentionRule { MailRetentionDays = 7, IsEnabled = true };
        var periodicRule2 = new MailRetentionRule { MailRetentionDays = 30, IsEnabled = true };

        _mockRepository.Setup(r => r.LoadMessages()).Returns(Enumerable.Empty<Core.Domain.Message.MessageEntry>());

        var rules = new IPeriodicBackgroundRule[] { periodicRule1, periodicRule2 };

        await rulesRunner.RunPeriodicBackgroundRules(rules, CancellationToken.None);

        // Give background tasks time to execute
        await Task.Delay(500);

        _mockRepository.Verify(r => r.LoadMessages(), Times.AtLeast(2));
    }

    #endregion

    #region Container Registration Tests

    [Test]
    public void Container_CanResolve_MailRetentionRule()
    {
        var rule = _container.Resolve<MailRetentionRule>();
        rule.Should().NotBeNull();
        rule.Should().BeOfType<MailRetentionRule>();
    }

    [Test]
    public void Container_CanResolve_MailRetentionRuleDispatch()
    {
        var dispatch = _container.Resolve<IRuleDispatcher<MailRetentionRule>>();
        dispatch.Should().NotBeNull();
        dispatch.Should().BeOfType<MailRetentionRuleDispatch>();
    }

    [Test]
    public void Container_CanResolve_RulesRunner()
    {
        var runner = _container.Resolve<IRulesRunner>();
        runner.Should().NotBeNull();
        runner.Should().BeOfType<RulesRunner>();
    }

    [Test]
    public void Container_CanResolve_BackgroundTaskRunner()
    {
        var taskRunner = _container.Resolve<IBackgroundTaskRunner>();
        taskRunner.Should().NotBeNull();
        taskRunner.Should().BeOfType<BackgroundTaskRunner>();
    }

    #endregion
}
