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

using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Search;

namespace Papercut.Helpers;

/// <summary>
/// Provides a modern, styled search panel for AvalonEdit text editors using XAML templates
/// </summary>
public static class ModernSearchPanel
{
    private static ControlTemplate? _cachedTemplate;

    /// <summary>
    /// Installs a modern search panel on the text editor with improved styling
    /// </summary>
    public static SearchPanel Install(TextEditor textEditor)
    {
        var searchPanel = SearchPanel.Install(textEditor);

        // Apply the modern XAML template
        ApplyModernTemplate(searchPanel);

        return searchPanel;
    }

    private static void ApplyModernTemplate(SearchPanel searchPanel)
    {
        // Load and cache the template once
        if (_cachedTemplate == null)
        {
            var resourceDict = new ResourceDictionary
            {
                Source = new Uri("pack://application:,,,/Papercut;component/Helpers/SearchPanelTemplate.xaml", UriKind.Absolute)
            };

            _cachedTemplate = resourceDict["ModernSearchPanelTemplate"] as ControlTemplate;
        }

        // Apply the template to the search panel
        if (_cachedTemplate != null)
        {
            searchPanel.Template = _cachedTemplate;
        }
    }
}
