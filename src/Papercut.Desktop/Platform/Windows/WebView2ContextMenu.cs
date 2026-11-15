// Papercut
//
// Copyright © 2008 - 2012 Ken Robertson
// Copyright © 2013 - 2026 Jaben Cargman
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


using System.Runtime.InteropServices;

using Microsoft.Web.WebView2.Core;

namespace Papercut.Desktop.Platform.Windows;

/// <summary>
/// Trims WebView2's browser menu to <see cref="ContextMenuPolicy"/>. The message list draws its
/// own menu in the page, so its right-clicks never get here.
/// </summary>
internal static class WebView2ContextMenu
{
    /// <summary>RO_E_CLOSED: the target went away (the pointer moved on) before we read it.</summary>
    const int TargetClosed = unchecked((int)0x8000000E);

    public static void Attach(CoreWebView2 core, Action<Uri> openLink)
    {
#if DEBUG
        const bool allowInspect = true;
#else
        const bool allowInspect = false;
#endif

        core.ContextMenuRequested += (_, e) =>
        {
            try
            {
                var target = e.ContextMenuTarget;

                var link = Read(() => target.HasLinkUri ? target.LinkUri : null);

                var plan = ContextMenuPolicy.Plan(
                    new ContextMenuTarget(
                        IsEditable: Read(() => target.IsEditable),
                        HasLink: !string.IsNullOrEmpty(link),
                        HasSelection: Read(() => target.HasSelection),
                        IsImage: Read(() => target.Kind == CoreWebView2ContextMenuTargetKind.Image),
                        InMainFrame: Read(() => target.IsRequestedForMainFrame, fallback: true)),
                    allowInspect);

                var builtIn = e.MenuItems
                    .Where(item => item.Kind != CoreWebView2ContextMenuItemKind.Separator)
                    .GroupBy(item => item.Name)
                    .ToDictionary(g => g.Key, g => g.First());

                var items = new List<CoreWebView2ContextMenuItem>();

                if (plan.OpenLink && Uri.TryCreate(link, UriKind.Absolute, out var uri))
                {
                    var open = core.Environment.CreateContextMenuItem("Open link", null, CoreWebView2ContextMenuItemKind.Command);
                    open.CustomItemSelected += (_, _) => openLink(uri);
                    items.Add(open);
                }

                items.AddRange(plan.Keep.Where(builtIn.ContainsKey).Select(name => builtIn[name]));

                e.MenuItems.Clear();

                if (items.Count == 0)
                {
                    // nothing useful here: no menu, rather than an empty one
                    e.Handled = true;
                    return;
                }

                foreach (var item in items) e.MenuItems.Add(item);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failure building the context menu");
            }
        };
    }

    static T Read<T>(Func<T> read, T fallback = default!)
    {
        try
        {
            return read();
        }
        catch (COMException ex) when (ex.HResult == TargetClosed)
        {
            return fallback;
        }
    }
}
