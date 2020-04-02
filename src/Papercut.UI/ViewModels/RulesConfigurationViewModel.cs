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

namespace Papercut.ViewModels
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;

    using Caliburn.Micro;

    using Papercut.Core.Annotations;
    using Papercut.Core.Domain.Rules;
    using Papercut.Services;

    public class RulesConfigurationViewModel : Screen
    {
        IRule _selectedRule;

        string _windowTitle = "Rules Configuration";

        public RulesConfigurationViewModel(RuleService ruleService, IEnumerable<IRule> registeredRules)
        {
            RegisteredRules = new ObservableCollection<IRule>(registeredRules);
            Rules = ruleService.Rules;
            Rules.CollectionChanged += (sender, args) =>
            {
                if (!Rules.Contains(SelectedRule))
                {
                    SelectedRule = null;
                }
            };
        }

        public string WindowTitle
        {
            get { return _windowTitle; }
            set
            {
                _windowTitle = value;
                NotifyOfPropertyChange(() => WindowTitle);
            }
        }

        public IRule SelectedRule
        {
            get { return _selectedRule; }
            set
            {
                _selectedRule = value;
                NotifyOfPropertyChange(() => SelectedRule);
                NotifyOfPropertyChange(() => HasSelectedRule);
            }
        }

        public bool HasSelectedRule => _selectedRule != null;

        public ObservableCollection<IRule> RegisteredRules { get; private set; }

        public void AddRule([NotNull] IRule rule)
        {
            if (rule == null) throw new ArgumentNullException(nameof(rule));

            var newRule = Activator.CreateInstance(rule.GetType()) as IRule;
            Rules.Add(newRule);
            SelectedRule = newRule;
        }

        public void DeleteRule()
        {
            if (SelectedRule != null) Rules.Remove(SelectedRule);
        }

        public ObservableCollection<IRule> Rules { get; private set; }
    }
}