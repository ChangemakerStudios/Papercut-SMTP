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

using Caliburn.Micro;

using Papercut.AppLayer.Rules;
using Papercut.Core.Domain.Rules;

namespace Papercut.ViewModels
{
    public class RulesConfigurationViewModel : Screen
    {
        IRule _selectedRule;

        string _windowTitle = "Rules Configuration";

        public RulesConfigurationViewModel(RuleService ruleService, IEnumerable<IRule> registeredRules)
        {
            this.RegisteredRules = new ObservableCollection<IRule>(registeredRules);
            this.Rules = ruleService.Rules;
            this.Rules.CollectionChanged += (sender, args) =>
            {
                if (!this.Rules.Contains(this.SelectedRule))
                {
                    this.SelectedRule = null;
                }
            };
        }

        public string WindowTitle
        {
            get => this._windowTitle;
            set
            {
                this._windowTitle = value;
                this.NotifyOfPropertyChange(() => this.WindowTitle);
            }
        }

        public IRule SelectedRule
        {
            get => this._selectedRule;
            set
            {
                this._selectedRule = value;
                this.NotifyOfPropertyChange(() => this.SelectedRule);
                this.NotifyOfPropertyChange(() => this.HasSelectedRule);
            }
        }

        public bool HasSelectedRule => this._selectedRule != null;

        public ObservableCollection<IRule> RegisteredRules { get; private set; }

        public ObservableCollection<IRule> Rules { get; }

        public void AddRule([NotNull] IRule rule)
        {
            if (rule == null) throw new ArgumentNullException(nameof(rule));

            var newRule = Activator.CreateInstance(rule.GetType()) as IRule;
            this.Rules.Add(newRule);
            this.SelectedRule = newRule;
        }

        public void DeleteRule()
        {
            if (this.SelectedRule != null) this.Rules.Remove(this.SelectedRule);
        }
    }
}