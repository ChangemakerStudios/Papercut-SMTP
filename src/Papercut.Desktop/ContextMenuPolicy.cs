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


namespace Papercut.Desktop;

/// <summary>What was right-clicked in the web view.</summary>
internal sealed record ContextMenuTarget(bool IsEditable, bool HasLink, bool HasSelection, bool IsImage, bool InMainFrame);

/// <summary>
/// The menu to show: our own "Open link" (through the link policy, like a click) and which of
/// the web view's built-in items to keep, in order. Nothing to show means no menu at all.
/// </summary>
internal sealed record ContextMenuPlan(bool OpenLink, IReadOnlyList<string> Keep)
{
    public bool IsEmpty => !OpenLink && Keep.Count == 0;
}

/// <summary>
/// The web view's own menu is a browser's -- Back, Refresh, Save as, Print -- and none of that
/// belongs in an app. Keep what helps with the thing under the pointer, the way Papercut 7's
/// message view did (Open Link, Copy Link, Copy, Select All), plus editing in text fields.
/// </summary>
internal static class ContextMenuPolicy
{
    /// <summary>The web view's names for its built-in items.</summary>
    static readonly string[] EditItems = ["undo", "redo", "cut", "copy", "paste", "pasteAndMatchStyle", "selectAll"];

    public static ContextMenuPlan Plan(ContextMenuTarget target, bool allowInspect)
    {
        var keep = new List<string>();
        var openLink = false;

        if (target.IsEditable)
        {
            keep.AddRange(EditItems);
        }
        else
        {
            if (target.HasLink)
            {
                openLink = true;
                keep.Add("copyLinkLocation");
            }

            if (target.HasSelection) keep.Add("copy");

            if (target.IsImage) keep.Add("copyImage");

            // the email, not the app around it: selecting all of the app's chrome helps nobody
            if (!target.InMainFrame && !target.HasLink) keep.Add("selectAll");
        }

        if (allowInspect) keep.Add("inspectElement");

        return new ContextMenuPlan(openLink, keep);
    }
}
