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
using Papercut.Rules.Domain.Invoking;
using Papercut.Rules.Domain.Rules;
using Papercut.Service.Application.Rules;
using Papercut.Service.Infrastructure.Configuration;

using Microsoft.Extensions.Configuration;

namespace Papercut.Service.Infrastructure.Rules;

public class RuleService(
    IRuleRepository ruleRepository,
    IBackgroundTaskRunner backgroundTaskRunner,
    ILogger logger,
    IRulesRunner rulesRunner,
    IConfiguration configuration)
    : RuleServiceBase(ruleRepository, logger, ServiceDataDirectory.GetFilePath(configuration, "rules.json")),
        IEventHandler<RulesUpdatedEvent>,
        IEventHandler<PapercutServiceReadyEvent>,
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
                        RuleUpdatePolicy.WithoutInvokeProcessRules(
                            Rules.OfType<INewMessageRule>(),
                            InvokeProcessRuleSettings.IsEnabled(configuration)),
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

    public Task HandleAsync(PapercutServiceReadyEvent @event, CancellationToken token = default)
    {
        try
        {
            // accessing "Rules" forces the collection to be loaded
            if (Rules.Any())
            {
                _logger.Information(
                    "Loaded {RuleCount} Rule(s) from {RuleFileName}",
                    Rules.Count,
                    RuleFileName);
            }
            else
            {
                _logger.Information("No Rules found in {RuleFileName}", RuleFileName);
            }

            WarnIfInvokeProcessRulesDisabled();
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

        WarnIfInvokeProcessRulesDisabled();

        return Task.CompletedTask;
    }

    private void WarnIfInvokeProcessRulesDisabled()
    {
        if (InvokeProcessRuleSettings.IsEnabled(configuration)) return;

        var count = Rules.OfType<InvokeProcessRule>().Count(r => r.IsEnabled);

        if (count == 0) return;

        _logger.Warning(
            "{RuleCount} Invoke Process rule(s) will not run: set {SettingKey}=true in the service configuration to allow them",
            count,
            InvokeProcessRuleSettings.EnabledSettingKey);
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
        _periodicRuleSubscription?.Dispose();

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
        // Singleton: this holds the process's rule state (loaded rules +
        // periodic subscription). A scoped instance would let HTTP-request
        // handling mutate a throwaway copy while the root instance keeps
        // dispatching the stale set (same failure mode as PapercutSmtpServer).
        builder.RegisterType<RuleService>().AsImplementedInterfaces().AsSelf()
            .SingleInstance();
    }

    #endregion
}