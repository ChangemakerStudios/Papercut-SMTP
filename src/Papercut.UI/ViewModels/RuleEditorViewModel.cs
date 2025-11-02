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
using System.ComponentModel;
using System.Reflection;

using Papercut.Core.Domain.Rules;

namespace Papercut.ViewModels;

public class RuleEditorViewModel : PropertyChangedBase
{
    private IRule? _selectedRule;

    public ObservableCollection<PropertyCategoryViewModel> PropertyCategories { get; } = new();

    public IRule? SelectedRule
    {
        get => _selectedRule;
        set
        {
            _selectedRule = value;
            NotifyOfPropertyChange(() => SelectedRule);
            LoadProperties();
        }
    }

    private void LoadProperties()
    {
        PropertyCategories.Clear();

        if (SelectedRule == null) return;

        var properties = TypeDescriptor.GetProperties(SelectedRule)
            .Cast<PropertyDescriptor>()
            .Where(p => p.IsBrowsable && !p.IsReadOnly)
            .OrderBy(p => p.Category)
            .ThenBy(p => p.DisplayName)
            .ToList();

        var categorizedProperties = properties
            .GroupBy(p => p.Category ?? "General")
            .OrderBy(g => g.Key == "Information" ? 0 : g.Key == "Settings" ? 1 : 2);

        foreach (var category in categorizedProperties)
        {
            var categoryVm = new PropertyCategoryViewModel(category.Key);

            foreach (var property in category)
            {
                var propertyVm = new PropertyViewModel(SelectedRule, property);
                categoryVm.Properties.Add(propertyVm);
            }

            PropertyCategories.Add(categoryVm);
        }
    }
}

public class PropertyCategoryViewModel(string categoryName)
{
    public string CategoryName { get; } = categoryName;

    public ObservableCollection<PropertyViewModel> Properties { get; } = new();
}

public class PropertyViewModel : PropertyChangedBase
{
    private readonly object _instance;
    private readonly PropertyDescriptor _property;

    public PropertyViewModel(object instance, PropertyDescriptor property)
    {
        _instance = instance;
        _property = property;

        // Subscribe to property changes if the instance implements INotifyPropertyChanged
        if (instance is INotifyPropertyChanged notifyPropertyChanged)
        {
            notifyPropertyChanged.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == _property.Name)
                {
                    NotifyOfPropertyChange(() => Value);
                    NotifyOfPropertyChange(() => NumericValue);
                }
            };
        }
    }

    public string DisplayName => _property.DisplayName ?? _property.Name;

    public string? Description => _property.Description;

    public bool HasDescription => !string.IsNullOrWhiteSpace(Description);

    public Type PropertyType => _property.PropertyType;

    public bool IsReadOnly => _property.IsReadOnly;

    public bool IsPassword => _property.Attributes.OfType<PasswordPropertyTextAttribute>().Any();

    public bool IsBoolean => PropertyType == typeof(bool);

    public bool IsInteger => PropertyType == typeof(int) || PropertyType == typeof(long);

    public bool IsString => PropertyType == typeof(string);

    public object? Value
    {
        get => _property.GetValue(_instance);
        set
        {
            if (value != Value)
            {
                _property.SetValue(_instance, value);
                NotifyOfPropertyChange(() => Value);
                NotifyOfPropertyChange(() => NumericValue);
            }
        }
    }

    public double? NumericValue
    {
        get
        {
            var val = _property.GetValue(_instance);
            if (val == null) return null;

            return val switch
            {
                int intVal => intVal,
                long longVal => longVal,
                double doubleVal => doubleVal,
                float floatVal => floatVal,
                _ => null
            };
        }
        set
        {
            if (value == null) return;

            object? convertedValue = PropertyType switch
            {
                Type t when t == typeof(int) => (int)value.Value,
                Type t when t == typeof(long) => (long)value.Value,
                Type t when t == typeof(double) => value.Value,
                Type t when t == typeof(float) => (float)value.Value,
                _ => null
            };

            if (convertedValue != null && convertedValue != Value)
            {
                _property.SetValue(_instance, convertedValue);
                NotifyOfPropertyChange(() => Value);
                NotifyOfPropertyChange(() => NumericValue);
            }
        }
    }
}
