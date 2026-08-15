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


using System.Windows.Controls;

using ICSharpCode.AvalonEdit;

namespace Papercut.Helpers;

public static class TextEditorContextMenu
{
    /// <summary>
    ///     Attaches a Copy / Select All context menu to a read-only AvalonEdit editor,
    ///     which provides no context menu of its own.
    /// </summary>
    public static void Attach(TextEditor editor)
    {
        var copyItem = new MenuItem { Header = "Copy", InputGestureText = "Ctrl+C" };
        copyItem.Click += (_, _) => editor.Copy();

        var selectAllItem = new MenuItem { Header = "Select All", InputGestureText = "Ctrl+A" };
        selectAllItem.Click += (_, _) => editor.SelectAll();

        var menu = new ContextMenu();
        menu.Items.Add(copyItem);
        menu.Items.Add(new Separator());
        menu.Items.Add(selectAllItem);

        menu.Opened += (_, _) => copyItem.IsEnabled = editor.SelectionLength > 0;

        editor.ContextMenu = menu;
    }
}
