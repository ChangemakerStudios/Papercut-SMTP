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


using System.Reactive.Concurrency;
using System.Reactive.Linq;

using Papercut.Core.Domain.BackgroundTasks;
using Papercut.Core.Domain.Rules;
using Papercut.Core.Infrastructure.Async;
using Papercut.Core.Infrastructure.Logging;
using Papercut.Rules.App;
using Papercut.Rules.Domain.Rules;

namespace Papercut.Service.Infrastructure.Rules;

public class RuleService(
    IRuleRepository ruleRepository,
    IBackgroundTaskRunner backgroundTaskRunner,
    ILogger logger,
    IRulesRunner rulesRunner)
    : RuleServiceBase(ruleRepository, logger),
        IEventHandler<RulesUpdatedEvent>,
        IEventHandler<PapercutClientReadyEvent>,
        IEventHandler<NewMessageEvent>
{
    private static readonly TimeSpan PeriodicRunInterval = TimeSpan.FromMinutes(1);

    private readonly CancellationTokenSource _cancellationTokenSource = new();

    private IDisposable? _periodicRuleSubscription;

    public Task HandleAsync(NewMessageEvent @event, CancellationToken token = default)
    {
        _logger.Information(
            "New Message {MessageFile} Arrived -- Running Rules",
            @event.NewMessage);

        backgroundTaskRunner.QueueBackgroundTask(
            async (t) =>
            {
                try
                {
                    await Task.Delay(2000, t);
                    await rulesRunner.RunNewMessageRules(
                        Rules.OfType<INewMessageRule>().ToArray(),
                        @event.NewMessage.ToEntry(),
                        t);
                }
                catch (ObjectDisposedException)
                {
                }
                catch (TaskCanceledException)
                {
                }
                catch (Exception ex) when (_logger.ErrorWithContext(ex, "Failure Running New Message Rules"))
                {
                }
            });

        return Task.CompletedTask;
    }

    public Task HandleAsync(PapercutClientReadyEvent @event, CancellationToken token = default)
    {
        _logger.Debug("Attempting to Load Rules from {RuleFileName} on AppReady", RuleFileName);

        try
        {
            // accessing "Rules" forces the collection to be loaded
            if (Rules.Any())
            {
                _logger.Information(
                    "Loaded {RuleCount} from {RuleFileName}",
                    Rules.Count,
                    RuleFileName);
            }
        }
        catch (Exception ex) when (_logger.ErrorWithContext(ex, "Error loading rules from file {RuleFileName}", RuleFileName))
        {
        }

        SetupPeriodicRuleObservable();

        return Task.CompletedTask;
    }

    public Task HandleAsync(RulesUpdatedEvent @event, CancellationToken token = default)
    {
        Rules.Clear();
        Rules.AddRange(@event.Rules);
        Save();

        return Task.CompletedTask;
    }

    protected override async ValueTask DisposeAsync(bool disposing)
    {
        await base.DisposeAsync(disposing);

        if (!disposing) return;

        try
        {
            await _cancellationTokenSource.CancelAsync();
            _periodicRuleSubscription?.Dispose();
        }
        catch (ObjectDisposedException)
        {
            // ignore
        }
        finally
        {
            _cancellationTokenSource.Dispose();
        }
    }

    private void SetupPeriodicRuleObservable()
    {
        _logger.Debug("Setting up Periodic Rule Observable {RunInterval}", PeriodicRunInterval);

        _periodicRuleSubscription = Observable.Interval(PeriodicRunInterval, TaskPoolScheduler.Default)
            .SubscribeAsync(
                async e => await rulesRunner.RunPeriodicBackgroundRules(
                    Rules.OfType<IPeriodicBackgroundRule>().ToArray(),
                    CancellationToken.None),
                ex =>
                {
                    // Only log if it's not a cancellation exception (which happens during shutdown)
                    if (ex is not OperationCanceledException and not TaskCanceledException)
                    {
                        _logger.Error(ex, "Error Running Periodic Rules");
                    }
                });
    }

    #region Begin Static Container Registrations

    private static void Register(ContainerBuilder builder)
    {
        builder.RegisterType<RuleService>().AsImplementedInterfaces().AsSelf()
            .InstancePerLifetimeScope();
    }

    #endregion
}