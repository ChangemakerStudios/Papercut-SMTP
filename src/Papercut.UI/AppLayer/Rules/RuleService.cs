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


using System.Reactive.Concurrency;
using System.Reactive.Linq;

using Autofac;

using Papercut.Common.Domain;
using Papercut.Core.Domain.Rules;
using Papercut.Core.Infrastructure.Async;
using Papercut.Domain.BackendService;
using Papercut.Domain.LifecycleHooks;
using Papercut.Helpers;
using Papercut.Message;
using Papercut.Rules.App;
using Papercut.Rules.Domain.Rules;

namespace Papercut.AppLayer.Rules
{
    public class RuleService : RuleServiceBase, IAppLifecycleStarted, IAppLifecyclePreExit
    {
        readonly IBackendServiceStatus _backendServiceStatus;

        readonly IMessageBus _messageBus;

        readonly MessageWatcher _messageWatcher;

        readonly IRulesRunner _rulesRunner;

        private IDisposable _rulesObservable;

        public RuleService(
            IRuleRepository ruleRepository,
            ILogger logger,
            IBackendServiceStatus backendServiceStatus,
            MessageWatcher messageWatcher,
            IRulesRunner rulesRunner,
            IMessageBus messageBus)
            : base(ruleRepository, logger)
        {
            this._backendServiceStatus = backendServiceStatus;
            this._messageWatcher = messageWatcher;
            this._rulesRunner = rulesRunner;
            this._messageBus = messageBus;
        }

        public Task<AppLifecycleActionResultType> OnPreExit()
        {
            this.Save();

            return Task.FromResult(AppLifecycleActionResultType.Continue);
        }

        public async Task OnStartedAsync()
        {
            this._logger.Information("Attempting to Load Rules from {RuleFileName} on AppReady", this.RuleFileName);

            try
            {
                // accessing "Rules" forces the collection to be loaded
                if (this.Rules.Any())
                {
                    this._logger.Information(
                        "Loaded {RuleCount} from {RuleFileName}",
                        this.Rules.Count,
                        this.RuleFileName);
                }
            }
            catch (Exception ex)
            {
                this._logger.Error(ex, "Error loading rules from file {RuleFileName}", this.RuleFileName);
            }

            // rules loaded/updated event
            await this._messageBus.PublishAsync(new RulesUpdatedEvent(this.Rules.ToArray()));

            this._rulesObservable = this.GetRuleChangedObservable(TaskPoolScheduler.Default)
                .SubscribeAsync(
                    async args =>
                    {
                        // TODO: here be bugs
                        if (args.EventArgs.NewItems != null)
                            this.HookPropertyChangedForRules(
                                args.EventArgs.NewItems.OfType<IRule>());

                        await this._messageBus.PublishAsync(
                            new RulesUpdatedEvent(this.Rules.ToArray()));
                    },
                    ex => this._logger.Error(ex, "Failure Publishing Rules"));

            this.HookPropertyChangedForRules(this.Rules);

            // the backend service handles rules running if it's online
            if (!this._backendServiceStatus.IsOnline)
            {
                this._logger.Debug("Setting up Rule Dispatcher Observable");

                // observe message watcher and run rules when a new message arrives
                this._messageWatcher.GetNewMessageObservable(TaskPoolScheduler.Default)
                    .DelaySubscription(TimeSpan.FromSeconds(1))
                    .SubscribeAsync(
                        async e => await this._rulesRunner.RunAsync(
                                       this.Rules.ToArray(),
                                       e.EventArgs.NewMessage),
                        ex => this._logger.Error(ex, "Error Running Rules on New Message"));
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                this._rulesObservable?.Dispose();
            }
        }

        void HookPropertyChangedForRules(IEnumerable<IRule> rules)
        {
            foreach (IRule m in rules)
            {
                m.GetPropertyChangedEvents(TaskPoolScheduler.Default)
                    .Subscribe(
                        async (_) =>
                            await this._messageBus.PublishAsync(
                                new RulesUpdatedEvent(this.Rules.ToArray())),
                        ex => this._logger.Error(ex, "Error Publishing Rules Updated Event"));
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
}