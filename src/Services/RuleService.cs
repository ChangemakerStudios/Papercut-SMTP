// Papercut
// 
// Copyright © 2008 - 2012 Ken Robertson
// Copyright © 2013 - 2014 Jaben Cargman
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
    using System.Collections.ObjectModel;
    using System.IO;
    using System.Linq;
    using System.Reactive.Concurrency;
    using System.Reactive.Linq;

    using Papercut.Core.Events;
    using Papercut.Core.Message;
    using Papercut.Core.Rules;

    using Serilog;

    public class RuleService : IHandleEvent<AppReadyEvent>, IHandleEvent<AppExitEvent>
    {
        readonly PapercutServiceBackendCoordinator _coordinator;

        readonly ILogger _logger;

        readonly MessageWatcher _messageWatcher;

        readonly IPublishEvent _publishEvent;

        readonly RuleRespository _ruleRespository;

        readonly Lazy<ObservableCollection<IRule>> _rules;

        readonly IRulesRunner _rulesRunner;

        public RuleService(
            RuleRespository ruleRespository,
            ILogger logger,
            PapercutServiceBackendCoordinator coordinator,
            MessageWatcher messageWatcher,
            IRulesRunner rulesRunner,
            IPublishEvent publishEvent)
        {
            _ruleRespository = ruleRespository;
            _logger = logger;
            _coordinator = coordinator;
            _messageWatcher = messageWatcher;
            _rulesRunner = rulesRunner;
            _publishEvent = publishEvent;
            RuleFileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "rules.json");
            _rules = new Lazy<ObservableCollection<IRule>>(GetRulesCollection);
        }

        public string RuleFileName { get; set; }

        public ObservableCollection<IRule> Rules
        {
            get { return _rules.Value; }
        }

        public void Handle(AppExitEvent @event)
        {
            Save();
        }

        public void Handle(AppReadyEvent @event)
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
            catch (Exception ex)
            {
                _logger.Error(ex, "Error loading rules from file {RuleFileName}", RuleFileName);
            }

            // rules loaded/updated event
            _publishEvent.Publish(new RulesUpdatedEvent(Rules.ToArray()));

            // publish event on rules changing
            Rules.CollectionChanged +=
                (sender, args) => _publishEvent.Publish(new RulesUpdatedEvent(Rules.ToArray()));

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

        protected virtual ObservableCollection<IRule> GetRulesCollection()
        {
            IList<IRule> loadRules = null;

            try
            {
                loadRules = _ruleRespository.LoadRules(RuleFileName);
            }
            catch (Exception ex)
            {
                _logger.Warning(ex, "Failed to load rules in file {RuleFileName}", RuleFileName);
            }

            return new ObservableCollection<IRule>(loadRules ?? new List<IRule>(0));
        }

        public void Save()
        {
            try
            {
                _ruleRespository.SaveRules(Rules, RuleFileName);
                _logger.Information(
                    "Saved {RuleCount} to {RuleFileName}",
                    Rules.Count,
                    RuleFileName);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error saving rules to file {RuleFileName}", RuleFileName);
            }
        }
    }
}