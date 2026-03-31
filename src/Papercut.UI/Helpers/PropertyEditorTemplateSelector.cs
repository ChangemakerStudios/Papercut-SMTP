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


using System.Windows;
using System.Windows.Controls;

using Papercut.ViewModels;

namespace Papercut.Helpers;

public class PropertyEditorTemplateSelector : DataTemplateSelector
{
    public DataTemplate? StringTemplate { get; set; }
    public DataTemplate? PasswordTemplate { get; set; }
    public DataTemplate? IntegerTemplate { get; set; }
    public DataTemplate? BooleanTemplate { get; set; }

    public override DataTemplate? SelectTemplate(object item, DependencyObject container)
    {
        if (item is not PropertyViewModel property)
            return base.SelectTemplate(item, container);

        if (property.IsPassword)
            return PasswordTemplate;

        if (property.IsBoolean)
            return BooleanTemplate;

        if (property.IsInteger)
            return IntegerTemplate;

        if (property.IsString)
            return StringTemplate;

        return base.SelectTemplate(item, container);
    }
}
