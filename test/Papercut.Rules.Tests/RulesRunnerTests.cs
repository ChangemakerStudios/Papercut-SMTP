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

using System.ComponentModel;
using AwesomeAssertions;
using Autofac;
using Moq;
using NUnit.Framework;
using Papercut.Core.Domain.Message;
using Papercut.Core.Domain.Rules;
using Papercut.Rules.App;
using Serilog;

namespace Papercut.Rules.Tests;

[TestFixture]
public class RulesRunnerTests
{
    private ILifetimeScope _scope = null!;
    private Mock<ILogger> _mockLogger = null!;
    private RulesRunner _runner = null!;
    private MessageEntry _testMessageEntry = null!;
    private TestRuleDispatcher _testDispatcher = null!;

    // Test rule implementation
    private class TestRule : IRule
    {
        public Guid Id { get; } = Guid.NewGuid();
        public bool IsEnabled { get; set; } = true;
        public string Type => "TestRule";
        public string Description => "Test rule for unit tests";
        public event PropertyChangedEventHandler? PropertyChanged;
    }

    // Test dispatcher implementation
    private class TestRuleDispatcher : IRuleDispatcher<TestRule>
    {
        public Func<TestRule, MessageEntry, CancellationToken, Task>? DispatchAction { get; set; }

        public async Task DispatchAsync(TestRule rule, MessageEntry messageEntry, CancellationToken token)
        {
            if (DispatchAction != null)
            {
                await DispatchAction(rule, messageEntry, token);
            }
        }
    }

    [SetUp]
    public void SetUp()
    {
        // Create a real Autofac container with test dispatcher
        var builder = new ContainerBuilder();
        _testDispatcher = new TestRuleDispatcher();
        builder.RegisterInstance(_testDispatcher).As<IRuleDispatcher<TestRule>>();
        _scope = builder.Build();

        _mockLogger = new Mock<ILogger>();
        _runner = new RulesRunner(_scope, _mockLogger.Object);

        var tempFile = Path.GetTempFileName();
        File.WriteAllText(tempFile, "test");
        _testMessageEntry = new MessageEntry(tempFile);
    }

    [TearDown]
    public void TearDown()
    {
        if (File.Exists(_testMessageEntry.File))
        {
            File.Delete(_testMessageEntry.File);
        }
        _scope?.Dispose();
    }

    #region Constructor Tests

    [Test]
    public void Constructor_WithValidParameters_CreatesInstance()
    {
        var action = () => new RulesRunner(_scope, _mockLogger.Object);
        action.Should().NotThrow();
    }

    #endregion

    #region Null Parameter Tests

    [Test]
    public void RunAsync_WithNullRules_ThrowsArgumentNullException()
    {
        var action = async () => await _runner.RunAsync(null!, _testMessageEntry, CancellationToken.None);
        action.Should().ThrowAsync<ArgumentNullException>();
    }

    [Test]
    public void RunAsync_WithNullMessageEntry_ThrowsArgumentNullException()
    {
        var rules = new IRule[] { new TestRule() };
        var action = async () => await _runner.RunAsync(rules, null!, CancellationToken.None);
        action.Should().ThrowAsync<ArgumentNullException>();
    }

    #endregion

    #region Basic Execution Tests

    [Test]
    public async Task RunAsync_WithEmptyRules_CompletesSuccessfully()
    {
        var rules = Array.Empty<IRule>();

        var action = async () => await _runner.RunAsync(rules, _testMessageEntry, CancellationToken.None);

        await action.Should().NotThrowAsync();
    }

    [Test]
    public async Task RunAsync_WithEnabledRule_DispatchesRule()
    {
        var rule = new TestRule { IsEnabled = true };
        var rules = new IRule[] { rule };
        var dispatched = false;

        _testDispatcher.DispatchAction = (r, m, t) =>
        {
            dispatched = true;
            return Task.CompletedTask;
        };

        await _runner.RunAsync(rules, _testMessageEntry, CancellationToken.None);

        dispatched.Should().BeTrue();
    }

    [Test]
    public async Task RunAsync_WithDisabledRule_DoesNotDispatch()
    {
        var rule = new TestRule { IsEnabled = false };
        var rules = new IRule[] { rule };
        var dispatched = false;

        _testDispatcher.DispatchAction = (r, m, t) =>
        {
            dispatched = true;
            return Task.CompletedTask;
        };

        await _runner.RunAsync(rules, _testMessageEntry, CancellationToken.None);

        dispatched.Should().BeFalse();
    }

    [Test]
    public async Task RunAsync_WithMultipleEnabledRules_DispatchesAll()
    {
        var rule1 = new TestRule { IsEnabled = true };
        var rule2 = new TestRule { IsEnabled = true };
        var rules = new IRule[] { rule1, rule2 };
        var dispatchCount = 0;

        _testDispatcher.DispatchAction = (r, m, t) =>
        {
            dispatchCount++;
            return Task.CompletedTask;
        };

        await _runner.RunAsync(rules, _testMessageEntry, CancellationToken.None);

        dispatchCount.Should().Be(2);
    }

    [Test]
    public async Task RunAsync_WithMixedEnabledDisabled_DispatchesOnlyEnabled()
    {
        var rule1 = new TestRule { IsEnabled = true };
        var rule2 = new TestRule { IsEnabled = false };
        var rule3 = new TestRule { IsEnabled = true };
        var rules = new IRule[] { rule1, rule2, rule3 };
        var dispatchCount = 0;

        _testDispatcher.DispatchAction = (r, m, t) =>
        {
            dispatchCount++;
            return Task.CompletedTask;
        };

        await _runner.RunAsync(rules, _testMessageEntry, CancellationToken.None);

        dispatchCount.Should().Be(2);
    }

    #endregion

    #region Parallel Execution Tests

    [Test]
    public async Task RunAsync_WithMultipleRules_ExecutesInParallel()
    {
        var rule1 = new TestRule { IsEnabled = true };
        var rule2 = new TestRule { IsEnabled = true };
        var rules = new IRule[] { rule1, rule2 };
        var executionTimes = new List<DateTime>();

        _testDispatcher.DispatchAction = async (r, m, t) =>
        {
            executionTimes.Add(DateTime.Now);
            await Task.Delay(100, t);
        };

        var startTime = DateTime.Now;
        await _runner.RunAsync(rules, _testMessageEntry, CancellationToken.None);
        var totalTime = DateTime.Now - startTime;

        // If executed in parallel, total time should be ~100ms, not ~200ms
        totalTime.TotalMilliseconds.Should().BeLessThan(150);
    }

    #endregion

    #region Cancellation Tests

    [Test]
    public void RunAsync_WithCancelledToken_ThrowsOperationCanceledException()
    {
        var rule = new TestRule { IsEnabled = true };
        var rules = new IRule[] { rule };
        var cts = new CancellationTokenSource();
        cts.Cancel();

        var action = async () => await _runner.RunAsync(rules, _testMessageEntry, cts.Token);
        action.Should().ThrowAsync<OperationCanceledException>();
    }

    [Test]
    public async Task RunAsync_WithTokenCancelledDuringExecution_StopsProcessing()
    {
        var rule1 = new TestRule { IsEnabled = true };
        var rule2 = new TestRule { IsEnabled = true };
        var rules = new IRule[] { rule1, rule2 };
        var cts = new CancellationTokenSource();

        _testDispatcher.DispatchAction = async (r, m, t) =>
        {
            await Task.Delay(50, t);
            cts.Cancel();
        };

        var action = async () => await _runner.RunAsync(rules, _testMessageEntry, cts.Token);
        await action.Should().ThrowAsync<OperationCanceledException>();
    }

    #endregion

    #region Error Handling Tests

    [Test]
    public async Task RunAsync_WithDispatcherThrowingException_LogsWarningAndContinues()
    {
        var rule = new TestRule { IsEnabled = true };
        var rules = new IRule[] { rule };

        _testDispatcher.DispatchAction = (r, m, t) =>
        {
            throw new InvalidOperationException("Test exception");
        };

        // Should not throw - exception is caught and logged
        await _runner.RunAsync(rules, _testMessageEntry, CancellationToken.None);

        _mockLogger.Verify(
            l => l.Warning(
                It.IsAny<Exception>(),
                It.Is<string>(s => s.Contains("Failure Dispatching Rule")),
                It.IsAny<TestRule>(),
                It.IsAny<MessageEntry>()),
            Times.Once);
    }

    [Test]
    public async Task RunAsync_WithOneRuleFailing_OtherRulesStillExecute()
    {
        var rule1 = new TestRule { IsEnabled = true };
        var rule2 = new TestRule { IsEnabled = true };
        var rules = new IRule[] { rule1, rule2 };
        var rule2Executed = false;
        var callCount = 0;

        _testDispatcher.DispatchAction = (r, m, t) =>
        {
            callCount++;
            if (callCount == 1)
            {
                throw new InvalidOperationException("First rule fails");
            }
            rule2Executed = true;
            return Task.CompletedTask;
        };

        await _runner.RunAsync(rules, _testMessageEntry, CancellationToken.None);

        rule2Executed.Should().BeTrue();
    }

    #endregion

    #region Logging Tests

    [Test]
    public async Task RunAsync_WithRule_LogsInformationBeforeDispatch()
    {
        var rule = new TestRule { IsEnabled = true };
        var rules = new IRule[] { rule };

        await _runner.RunAsync(rules, _testMessageEntry, CancellationToken.None);

        _mockLogger.Verify(
            l => l.Information(
                It.Is<string>(s => s.Contains("Running Rule Dispatch")),
                It.IsAny<TestRule>(),
                It.IsAny<MessageEntry>()),
            Times.Once);
    }

    #endregion
}
