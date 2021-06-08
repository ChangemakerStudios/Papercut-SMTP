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


namespace Papercut.Rules
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Collections.Specialized;
    using System.IO;
    using System.Reactive;
    using System.Reactive.Concurrency;
    using System.Reactive.Linq;

    using Autofac.Util;

    using Papercut.Core.Domain.Rules;

    using Serilog;

    public class RuleServiceBase : Disposable
    {
        readonly Lazy<ObservableCollection<IRule>> _rules;

        protected readonly ILogger Logger;

        protected readonly RuleRepository RuleRepository;

        protected RuleServiceBase(RuleRepository ruleRepository, ILogger logger)
        {
            this.RuleRepository = ruleRepository;
            this.Logger = logger;
            this.RuleFileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "rules.json");
            this._rules = new Lazy<ObservableCollection<IRule>>(this.GetRulesCollection);
        }

        public string RuleFileName { get; set; }

        public ObservableCollection<IRule> Rules => this._rules.Value;

        public IObservable<EventPattern<NotifyCollectionChangedEventArgs>> GetRuleChangedObservable(IScheduler scheduler = null)
        {
            return Observable
                .FromEventPattern<NotifyCollectionChangedEventHandler,
                    NotifyCollectionChangedEventArgs>(
                    h => new NotifyCollectionChangedEventHandler(h),
                    h => this._rules.Value.CollectionChanged += h,
                    h => this._rules.Value.CollectionChanged -= h,
                    scheduler ?? Scheduler.Default);
        }

        protected virtual ObservableCollection<IRule> GetRulesCollection()
        {
            IList<IRule> loadRules = null;

            try
            {
                loadRules = this.RuleRepository.LoadRules(this.RuleFileName);
            }
            catch (Exception ex)
            {
                this.Logger.Warning(ex, "Failed to load rules in file {RuleFileName}", this.RuleFileName);
            }

            return new ObservableCollection<IRule>(loadRules ?? new List<IRule>(0));
        }

        public void Save()
        {
            try
            {
                this.RuleRepository.SaveRules(this.Rules, this.RuleFileName);
                this.Logger.Information(
                    "Saved {RuleCount} to {RuleFileName}",
                    this.Rules.Count,
                    this.RuleFileName);
            }
            catch (Exception ex)
            {
                this.Logger.Error(ex, "Error saving rules to file {RuleFileName}", this.RuleFileName);
            }
        }
    }
}