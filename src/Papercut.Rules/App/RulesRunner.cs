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


using System.Reflection;

using Autofac;

using Papercut.Core.Domain.BackgroundTasks;
using Papercut.Core.Domain.Message;
using Papercut.Core.Domain.Rules;

namespace Papercut.Rules.App;

public class RulesRunner : IRulesRunner
{
    readonly IBackgroundTaskRunner _backgroundTaskRunner;

    readonly MethodInfo _dispatchRuleMethod;

    readonly ILifetimeScope _lifetimeScope;

    readonly ILogger _logger;

    public RulesRunner(ILifetimeScope lifetimeScope, IBackgroundTaskRunner backgroundTaskRunner, ILogger logger)
    {
        _lifetimeScope = lifetimeScope;
        _backgroundTaskRunner = backgroundTaskRunner;
        _logger = logger;

        var dispatchRuleMethod = GetType()
            .GetMethod(
                nameof(DispatchRuleAsync),
                BindingFlags.NonPublic | BindingFlags.Instance);

        if (dispatchRuleMethod == null)
        {
            throw new ArgumentNullException(nameof(dispatchRuleMethod), "Dispatch rule method is null");
        }

        _dispatchRuleMethod = dispatchRuleMethod;
    }

    public async Task RunNewMessageRules(INewMessageRule[] rules, MessageEntry messageEntry,
        CancellationToken token = default)
    {
        if (rules == null) throw new ArgumentNullException(nameof(rules));
        if (messageEntry == null) throw new ArgumentNullException(nameof(messageEntry));

        var ruleTasks = new List<Task>();

        foreach (var rule in rules.Where(r => r.IsEnabled))
        {
            token.ThrowIfCancellationRequested();

            var invoke = _dispatchRuleMethod.MakeGenericMethod(rule.GetType()).Invoke(
                this,
                [rule, messageEntry, token]);

            if (invoke is Task invokeTask)
            {
                ruleTasks.Add(invokeTask);
            }
        }

        await Task.WhenAll(ruleTasks).WaitAsync(token);
    }

    public Task RunPeriodicBackgroundRules(IPeriodicBackgroundRule[] rules, CancellationToken token = default)
    {
        if (rules == null) throw new ArgumentNullException(nameof(rules));

        var ruleTasks = new List<Task>();

        foreach (var rule in rules.Where(r => r.IsEnabled))
        {
            token.ThrowIfCancellationRequested();

            var invoke = _dispatchRuleMethod.MakeGenericMethod(rule.GetType()).Invoke(
                this,
                [rule, null, token]);

            if (invoke is Task invokeTask)
            {
                ruleTasks.Add(invokeTask);
            }
        }

        _backgroundTaskRunner.QueueBackgroundTask(
            async (t) => await Task.WhenAll(ruleTasks).WaitAsync(t));

        return Task.CompletedTask;
    }

    [UsedImplicitly]
    async Task DispatchRuleAsync<TRule>(TRule rule, MessageEntry? messageEntry, CancellationToken token)
        where TRule : IRule
    {
        if (rule is INewMessageRule)
        {
            _logger.Information(
                "Running Rule Dispatch for Rule {Rule} on Message {@MessageEntry}",
                rule,
                messageEntry);
        }
        else if (rule is IPeriodicBackgroundRule)
        {
            _logger.Debug(
                "Running Periodic Background for Rule {Rule}",
                rule);
        }

        try
        {
            var ruleDispatcher = _lifetimeScope.Resolve<IRuleDispatcher<TRule>>();
            await ruleDispatcher.DispatchAsync(rule, messageEntry, token);
        }
        catch (Exception ex)
        {
            _logger.Warning(
                ex,
                "Failure Dispatching Rule {Rule} on Message {@MessageEntry}",
                rule,
                messageEntry);
        }
    }

    #region Begin Static Container Registrations

    /// <summary>
    /// Called dynamically from the RegisterStaticMethods() call in the container module.
    /// </summary>
    /// <param name="builder"></param>
    [UsedImplicitly]
    static void Register(ContainerBuilder builder)
    {
        if (builder == null) throw new ArgumentNullException(nameof(builder));

        builder.RegisterType<RulesRunner>().As<IRulesRunner>().SingleInstance();
    }

    #endregion
}