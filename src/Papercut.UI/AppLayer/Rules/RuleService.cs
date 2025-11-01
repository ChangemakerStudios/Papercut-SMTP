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


namespace Papercut.AppLayer.Rules;

using Papercut.Core.Domain.Rules;
using Papercut.Core.Infrastructure.Async;
using Papercut.Domain.BackendService;
using Papercut.Message;
using Papercut.Rules.App;
using Papercut.Rules.Domain.Rules;

public class RuleService(
    IRuleRepository ruleRepository,
    ILogger logger,
    IBackendServiceStatus backendServiceStatus,
    MessageWatcher messageWatcher,
    IRulesRunner rulesRunner,
    IMessageBus messageBus)
    : RuleServiceBase(ruleRepository, logger), IAppLifecycleStarted, IAppLifecyclePreExit, IEventHandler<PapercutServiceStatusEvent>
{
    private static readonly TimeSpan PeriodicRunInterval = TimeSpan.FromMinutes(1);

    private readonly Stack<IDisposable> _ruleDisposables = new();

    private readonly Stack<IDisposable> _ruleObservableDisposables = new();

    public Task<AppLifecycleActionResultType> OnPreExit()
    {
        Save();

        return Task.FromResult(AppLifecycleActionResultType.Continue);
    }

    public async Task OnStartedAsync()
    {
        await LoadRules();
    }

    public async Task HandleAsync(PapercutServiceStatusEvent @event, CancellationToken token = default)
    {
        _logger.Information("Papercut Service is {NewStatus}", @event.PapercutServiceStatus);

        await SyncRuleObservables();
    }

    private async Task LoadRules()
    {
        _logger.Information("Attempting to Load Rules from {RuleFileName} on AppReady", RuleFileName);

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
        catch (Exception ex)
        {
            _logger.Error(ex, "Error loading rules from file {RuleFileName}", RuleFileName);
        }

        await SetupPropertyChangeObservablesForAllRules();

        // rules loaded/updated event
        await messageBus.PublishAsync(new RulesUpdatedEvent(Rules.ToArray()));
    }

    private async Task SyncRuleObservables()
    {
        await CleanupRuleSubscriptions();

        if (backendServiceStatus.IsOnline)
        {
            _logger.Information("Backend service is online - rule execution delegated to service");
            return;
        }

        _logger.Information("Rule subscriptions will be run in UI since backend service is offline");

        var cancellationSource = new CancellationTokenSource();
        _ruleObservableDisposables.Push(cancellationSource);

        _ruleObservableDisposables.Push(GetRuleChangedObservable(TaskPoolScheduler.Default)
            .SubscribeAsync(
                async args =>
                {
                    if (args.EventArgs.NewItems != null)
                    {
                        await SetupPropertyChangeObservablesForAllRules();
                    }

                    await messageBus.PublishAsync(new RulesUpdatedEvent(Rules.ToArray()), cancellationSource.Token);
                },
                ex => _logger.Error(ex, "Failure Publishing Rules")));

        _logger.Debug("Setting up Rule Dispatcher Observable");

        // observe message watcher and run rules when a new message arrives
        _ruleObservableDisposables.Push(messageWatcher.GetNewMessageObservable(TaskPoolScheduler.Default)
            .DelaySubscription(TimeSpan.FromSeconds(1))
            .SubscribeAsync(
                async e => await rulesRunner.RunNewMessageRules(
                    Rules.OfType<INewMessageRule>().ToArray(),
                    e.EventArgs.NewMessage,
                    cancellationSource.Token),
                ex =>
                {
                    _logger.Error(ex, "Error Running Rules on New Message");
                }));

        _logger.Debug("Setting up Periodic Rule Observable {RunInterval}", PeriodicRunInterval);

        _ruleObservableDisposables.Push(Observable.Interval(PeriodicRunInterval, TaskPoolScheduler.Default)
            .SubscribeAsync(
                async e => await rulesRunner.RunPeriodicBackgroundRules(
                    Rules.OfType<IPeriodicBackgroundRule>().ToArray(),
                    cancellationSource.Token),
                ex =>
                {
                    _logger.Error(ex, "Error Running Periodic Rules");
                }));
    }

    private async Task SetupPropertyChangeObservablesForAllRules()
    {
        await CleanupPropertyChangeSubscriptions();

        foreach (var m in Rules)
        {
            _ruleDisposables.Push(m.GetPropertyChangedEvents(TaskPoolScheduler.Default)
                .SubscribeAsync(
                    async (_) =>
                        await messageBus.PublishAsync(
                            new RulesUpdatedEvent(Rules.ToArray())),
                    ex => _logger.Error(ex, "Error Publishing Rules Updated Event")));
        }
    }

    protected override async ValueTask DisposeAsync(bool disposing)
    {
        if (!disposing) return;

        try
        {
            await (CleanupRuleSubscriptions(), CleanupPropertyChangeSubscriptions());
        }
        catch (ObjectDisposedException)
        {
            // ignore
        }
    }

    private Task CleanupRuleSubscriptions()
    {
        try
        {
            while (_ruleObservableDisposables.TryPop(out var disposable))
            {
                disposable.Dispose();
            }
        }
        catch (ObjectDisposedException)
        {
            // ignore
        }

        return Task.CompletedTask;
    }

    private Task CleanupPropertyChangeSubscriptions()
    {
        try
        {
            while (_ruleDisposables.TryPop(out var disposable))
            {
                disposable.Dispose();
            }
        }
        catch (ObjectDisposedException)
        {
            // ignore
        }

        return Task.CompletedTask;
    }

    #region Begin Static Container Registrations

    /// <summary>
    /// Called dynamically from the RegisterStaticMethods() call in the container module.
    /// </summary>
    /// <param name="builder"></param>
    [UsedImplicitly]
    private static void Register(ContainerBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RegisterType<RuleService>().AsSelf().AsImplementedInterfaces()
            .InstancePerLifetimeScope();
    }

    #endregion
}