// Papercut
// 
// Copyright © 2008 - 2012 Ken Robertson
// Copyright © 2013 - 2021 Jaben Cargman
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

namespace Papercut.AppLayer.Rules
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Linq;
    using System.Reactive.Concurrency;
    using System.Reactive.Linq;
    using System.Threading.Tasks;

    using Autofac;

    using Papercut.Common.Domain;
    using Papercut.Core.Annotations;
    using Papercut.Core.Domain.Message;
    using Papercut.Core.Domain.Rules;
    using Papercut.Domain.BackendService;
    using Papercut.Domain.LifecycleHooks;
    using Papercut.Message;
    using Papercut.Rules;

    using Serilog;

    public class RuleService : RuleServiceBase, IAppLifecycleStarted, IAppLifecyclePreExit
    {
        readonly IBackendServiceStatus _backendServiceStatus;

        readonly IMessageBus _messageBus;

        readonly MessageWatcher _messageWatcher;

        readonly IRulesRunner _rulesRunner;

        public RuleService(
            RuleRepository ruleRepository,
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

        public AppLifecycleActionResultType OnPreExit()
        {
            this.Save();

            return AppLifecycleActionResultType.Continue;
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

            this.Rules.CollectionChanged += this.RuleCollectionChanged;
            this.HookPropertyChangedForRules(this.Rules);

            // the backend service handles rules running if it's online
            if (!this._backendServiceStatus.IsOnline)
            {
                this._logger.Debug("Setting up Rule Dispatcher Observable");

                // observe message watcher and run rules when a new message arrives
                Observable.FromEventPattern<NewMessageEventArgs>(
                        e => this._messageWatcher.NewMessage += e,
                        e => this._messageWatcher.NewMessage -= e,
                        TaskPoolScheduler.Default)
                    .DelaySubscription(TimeSpan.FromSeconds(1))
                    .Subscribe(
                        async e => await this._rulesRunner.RunAsync(
                                       this.Rules.ToArray(),
                                       e.EventArgs.NewMessage));
            }
        }

        void RuleCollectionChanged(object sender, NotifyCollectionChangedEventArgs args)
        {
            try
            {
                if (args.NewItems != null) this.HookPropertyChangedForRules(args.NewItems.OfType<IRule>());

                this._messageBus.PublishFireAndForget(new RulesUpdatedEvent(this.Rules.ToArray()));
            }
            catch (Exception ex)
            {
                this._logger.Error(ex, "Failure Handling Rule Collection Change {@Args}", args);
            }
        }

        void HookPropertyChangedForRules(IEnumerable<IRule> rules)
        {
            foreach (IRule m in rules)
            {
                m.PropertyChanged += (o, eventArgs) =>
                    this._messageBus.PublishAsync(new RulesUpdatedEvent(this.Rules.ToArray()));
            }
        }

        #region Begin Static Container Registrations

        /// <summary>
        /// Called dynamically from the RegisterStaticMethods() call in the container module.
        /// </summary>
        /// <param name="builder"></param>
        [UsedImplicitly]
        static void Register([NotNull] ContainerBuilder builder)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));

            builder.RegisterType<RuleService>().AsSelf().AsImplementedInterfaces()
                .InstancePerLifetimeScope();
        }

        #endregion
    }
}