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


using Papercut.Core.Domain.Rules;
using Papercut.Core.Infrastructure.Async;
using Papercut.Domain.BackendService;
using Papercut.Message;
using Papercut.Rules.App;
using Papercut.Rules.Domain.Rules;

namespace Papercut.AppLayer.Rules;

public class RuleService : RuleServiceBase, IAppLifecycleStarted, IAppLifecyclePreExit, IEventHandler<PapercutServiceStatusEvent>
{
    readonly IBackendServiceStatus _backendServiceStatus;

    readonly IMessageBus _messageBus;

    readonly MessageWatcher _messageWatcher;

    readonly IRulesRunner _rulesRunner;

    private IDisposable? _newMessageRuleSubscription;

    private IDisposable? _periodicRuleSubscription;

    private IDisposable? _ruleChangedObservable;

    private readonly CancellationTokenSource _cancellationTokenSource = new();

    private readonly Stack<IDisposable> _ruleDisposables = new();

    public RuleService(
        IRuleRepository ruleRepository,
        ILogger logger,
        IBackendServiceStatus backendServiceStatus,
        MessageWatcher messageWatcher,
        IRulesRunner rulesRunner,
        IMessageBus messageBus)
        : base(ruleRepository, logger)
    {
        _backendServiceStatus = backendServiceStatus;
        _messageWatcher = messageWatcher;
        _rulesRunner = rulesRunner;
        _messageBus = messageBus;
    }

    public Task<AppLifecycleActionResultType> OnPreExit()
    {
        Save();

        return Task.FromResult(AppLifecycleActionResultType.Continue);
    }

    public async Task OnStartedAsync()
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
        await _messageBus.PublishAsync(new RulesUpdatedEvent(Rules.ToArray()));

        // the backend service handles rules running if it's online
        if (!_backendServiceStatus.IsOnline)
        {
            await SetupRuleObservables();
        }
    }

    public async Task HandleAsync(PapercutServiceStatusEvent @event, CancellationToken token = default)
    {
        this._logger.Information("Papercut Service is now {NewStatus}",
            @event.PapercutServiceStatus);

        await this.CleanupRuleSubscriptions();
        await this.SetupPropertyChangeObservablesForAllRules();
    }

    private Task SetupRuleObservables()
    {
        if (_backendServiceStatus.IsOnline)
        {
            return Task.CompletedTask;
        }

        _ruleChangedObservable = GetRuleChangedObservable(TaskPoolScheduler.Default)
            .SubscribeAsync(
                async args =>
                {
                    if (args.EventArgs.NewItems != null)
                    {
                        await SetupPropertyChangeObservablesForAllRules();
                    }

                    await _messageBus.PublishAsync(new RulesUpdatedEvent(Rules.ToArray()), _cancellationTokenSource.Token);
                },
                ex => _logger.Error(ex, "Failure Publishing Rules"));

        _logger.Debug("Setting up Rule Dispatcher Observable");

        // observe message watcher and run rules when a new message arrives
        _newMessageRuleSubscription = _messageWatcher.GetNewMessageObservable(TaskPoolScheduler.Default)
            .DelaySubscription(TimeSpan.FromSeconds(1))
            .SubscribeAsync(
                async e => await _rulesRunner.RunNewMessageRules(
                    Rules.OfType<INewMessageRule>().ToArray(),
                    e.EventArgs.NewMessage, _cancellationTokenSource.Token),
                ex => _logger.Error(ex, "Error Running Rules on New Message"));

        _logger.Debug("Setting up Periodic Rule Observable");

        _periodicRuleSubscription = Observable.Interval(TimeSpan.FromMinutes(1), TaskPoolScheduler.Default)
            .SubscribeAsync(
                async e => await _rulesRunner.RunPeriodicBackgroundRules(
                    Rules.OfType<IPeriodicBackgroundRule>().ToArray(), _cancellationTokenSource.Token),
                ex => _logger.Error(ex, "Error Running Period Rules"));

        return Task.CompletedTask;
    }

    protected override async ValueTask DisposeAsync(bool disposing)
    {
        if (!disposing) return;

        try
        {
            await (this.CleanupRuleSubscriptions(), this.CleanupPropertyChangeSubscriptions());
        }
        catch (ObjectDisposedException)
        {
            // ignore
        }
    }

    private async Task CleanupRuleSubscriptions()
    {
        try
        {
            await _cancellationTokenSource.CancelAsync();

            _ruleChangedObservable?.Dispose();
            _newMessageRuleSubscription?.Dispose();
            _periodicRuleSubscription?.Dispose();
        }
        catch (ObjectDisposedException)
        {
            // ignore
        }
        finally
        {
            _cancellationTokenSource.TryReset();
        }
    }

    private Task CleanupPropertyChangeSubscriptions()
    {
        try
        {
            while (this._ruleDisposables.TryPop(out var disposable))
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

    private async Task SetupPropertyChangeObservablesForAllRules()
    {
        await CleanupPropertyChangeSubscriptions();

        foreach (var m in this.Rules)
        {
            _ruleDisposables.Push(m.GetPropertyChangedEvents(TaskPoolScheduler.Default)
                .SubscribeAsync(
                    async (_) =>
                        await _messageBus.PublishAsync(
                            new RulesUpdatedEvent(Rules.ToArray())),
                    ex => _logger.Error(ex, "Error Publishing Rules Updated Event")));
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
        ArgumentNullException.ThrowIfNull(builder);

        builder.RegisterType<RuleService>().AsSelf().AsImplementedInterfaces()
            .InstancePerLifetimeScope();
    }

    #endregion
}