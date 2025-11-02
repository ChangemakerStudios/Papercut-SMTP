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

using Papercut.Core.Domain.Rules;

namespace Papercut.ViewModels;

public class RuleTypeSelectionViewModel(IEnumerable<IRule> availableRuleTypes) : Screen
{
    private IRule? _selectedRuleType;

    public ObservableCollection<IRule> AvailableRuleTypes { get; } = new(availableRuleTypes);

    public IRule? SelectedRuleType
    {
        get => _selectedRuleType;
        set
        {
            _selectedRuleType = value;
            NotifyOfPropertyChange(() => SelectedRuleType);
            NotifyOfPropertyChange(() => CanAdd);
        }
    }

    public bool CanAdd => SelectedRuleType != null;

    public void Add()
    {
        if (SelectedRuleType != null)
        {
            TryCloseAsync(true);
        }
    }

    public void Cancel()
    {
        TryCloseAsync(false);
    }
}
