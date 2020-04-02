// Papercut
// 
// Copyright © 2008 - 2012 Ken Robertson
// Copyright © 2013 - 2020 Jaben Cargman
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

namespace Papercut.Services
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Linq;
    using System.Reactive.Concurrency;
    using System.Reactive.Linq;
    using System.Threading.Tasks;

    using Papercut.Common.Domain;
    using Papercut.Core.Domain.Message;
    using Papercut.Core.Domain.Rules;
    using Papercut.Core.Infrastructure.Lifecycle;
    using Papercut.Message;
    using Papercut.Rules;

    using Serilog;

    public class RuleService : RuleServiceBase,
        IEventHandler<PapercutClientReadyEvent>,
        IEventHandler<PapercutClientExitEvent>
    {
        readonly PapercutServiceBackendCoordinator _coordinator;

        readonly MessageWatcher _messageWatcher;

        readonly IMessageBus _messageBus;

        readonly IRulesRunner _rulesRunner;

        public RuleService(
            RuleRepository ruleRepository,
            ILogger logger,
            PapercutServiceBackendCoordinator coordinator,
            MessageWatcher messageWatcher,
            IRulesRunner rulesRunner,
            IMessageBus messageBus)
            : base(ruleRepository, logger)
        {
            _coordinator = coordinator;
            _messageWatcher = messageWatcher;
            _rulesRunner = rulesRunner;
            this._messageBus = messageBus;
        }

        public void Handle(PapercutClientExitEvent @event)
        {
            Save();
        }

        public void Handle(PapercutClientReadyEvent @event)
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

            // rules loaded/updated event
            this._messageBus.Publish(new RulesUpdatedEvent(this.Rules.ToArray()));

            Rules.CollectionChanged += RuleCollectionChanged;
            HookPropertyChangedForRules(Rules);

            // the backend service handles rules running if it's online
            if (!_coordinator.IsBackendServiceOnline)
            {
                _logger.Debug("Setting up Rule Dispatcher Observable");

                // observe message watcher and run rules when a new message arrives
                Observable.FromEventPattern<NewMessageEventArgs>(
                    e => _messageWatcher.NewMessage += e,
                    e => _messageWatcher.NewMessage -= e,
                    TaskPoolScheduler.Default)
                    .DelaySubscription(TimeSpan.FromSeconds(1))
                    .Subscribe(e => _rulesRunner.Run(Rules.ToArray(), e.EventArgs.NewMessage));
            }
        }

        void PublishUpdateEventAsync()
        {
            this._messageBus.Publish(new RulesUpdatedEvent(this.Rules.ToArray()));
        }

        void RuleCollectionChanged(object sender, NotifyCollectionChangedEventArgs args)
        {
            try
            {
                if (args.NewItems != null)
                    HookPropertyChangedForRules(args.NewItems.OfType<IRule>());

                this.PublishUpdateEventAsync();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failure Handling Rule Collection Change {@Args}", args);
            }
        }

        void HookPropertyChangedForRules(IEnumerable<IRule> rules)
        {
            foreach (IRule m in rules)
            {
                m.PropertyChanged += (o, eventArgs) => this.PublishUpdateEventAsync();
            }
        }
    }
}