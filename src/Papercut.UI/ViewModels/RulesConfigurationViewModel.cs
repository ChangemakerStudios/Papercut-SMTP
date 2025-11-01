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


using System.Collections.ObjectModel;

using Papercut.AppLayer.Rules;
using Papercut.Core.Domain.Rules;

namespace Papercut.ViewModels;

public class RulesConfigurationViewModel : Screen
{
    private IRule? _selectedRule;

    private string _windowTitle = "Rules Configuration";

    public RulesConfigurationViewModel(RuleService ruleService, IEnumerable<IRule> registeredRules)
    {
        RegisteredRules = new ObservableCollection<IRule>(registeredRules);
        Rules = ruleService.Rules;
        Rules.CollectionChanged += (_, _) =>
        {
            if (!Rules.Contains(SelectedRule))
            {
                SelectedRule = null;
            }
        };
    }

    public string WindowTitle
    {
        get => _windowTitle;
        set
        {
            _windowTitle = value;
            NotifyOfPropertyChange(() => WindowTitle);
        }
    }

    public IRule? SelectedRule
    {
        get => _selectedRule;
        set
        {
            _selectedRule = value;
            NotifyOfPropertyChange(() => SelectedRule);
            NotifyOfPropertyChange(() => HasSelectedRule);
        }
    }

    public bool HasSelectedRule => _selectedRule != null;

    public ObservableCollection<IRule> RegisteredRules { get; private set; }

    public ObservableCollection<IRule> Rules { get; }

    public void AddRule(IRule rule)
    {
        ArgumentNullException.ThrowIfNull(rule);

        if (Activator.CreateInstance(rule.GetType()) is IRule newRule)
        {
            Rules.Add(newRule);
            SelectedRule = newRule;
        }
    }

    public void DeleteRule()
    {
        if (SelectedRule != null) Rules.Remove(SelectedRule);
    }
}