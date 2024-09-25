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


using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;

using Autofac.Util;

using Papercut.Core.Domain.Rules;
using Papercut.Rules.Domain.Rules;

namespace Papercut.Rules.App;

public class RuleServiceBase : Disposable
{
    protected readonly ILogger _logger;

    protected readonly IRuleRepository _ruleRepository;

    readonly Lazy<ObservableCollection<IRule>> _rules;

    protected RuleServiceBase(IRuleRepository ruleRepository, ILogger logger)
    {
        this._ruleRepository = ruleRepository;
        this._logger = logger;
        this.RuleFileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "rules.json");
        this._rules = new Lazy<ObservableCollection<IRule>>(this.GetRulesCollection);
    }

    public string RuleFileName { get; set; }

    public ObservableCollection<IRule> Rules => this._rules.Value;

    public IObservable<EventPattern<NotifyCollectionChangedEventArgs>> GetRuleChangedObservable(IScheduler? scheduler = null)
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
        IList<IRule>? loadRules = null;

        try
        {
            loadRules = this._ruleRepository.LoadRules(this.RuleFileName);
        }
        catch (Exception ex)
        {
            this._logger.Warning(ex, "Failed to load rules in file {RuleFileName}", this.RuleFileName);
        }

        return new ObservableCollection<IRule>(loadRules ?? new List<IRule>(0));
    }

    public void Save()
    {
        try
        {
            this._ruleRepository.SaveRules(this.Rules, this.RuleFileName);
            this._logger.Information(
                "Saved {RuleCount} to {RuleFileName}",
                this.Rules.Count,
                this.RuleFileName);
        }
        catch (Exception ex)
        {
            this._logger.Error(ex, "Error saving rules to file {RuleFileName}", this.RuleFileName);
        }
    }
}