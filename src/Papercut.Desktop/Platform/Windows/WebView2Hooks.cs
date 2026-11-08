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


using Avalonia.Controls;
using Avalonia.Platform;

using Microsoft.Web.WebView2.Core;

namespace Papercut.Desktop.Platform.Windows;

/// <summary>
/// Avalonia's WebView2 adapter only wires NavigationStarting (main frame), so iframe navigations
/// and permission prompts are hooked on the raw CoreWebView2. Verified by the shell spike.
/// </summary>
internal static class WebView2Hooks
{
    public static void Attach(NativeWebView webView, Func<Uri, bool> isAllowed)
    {
        if (webView.TryGetPlatformHandle() is not IWindowsWebView2PlatformHandle handle || handle.CoreWebView2 == IntPtr.Zero)
        {
            Log.Error("No CoreWebView2 handle -- iframe navigations are not covered by the link policy");
            return;
        }

        CoreWebView2 core;

        try
        {
            core = CoreWebView2.CreateFromComICoreWebView2(handle.CoreWebView2);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Unable to wrap the CoreWebView2 -- iframe navigations are not covered by the link policy");
            return;
        }

        core.FrameNavigationStarting += (_, e) =>
        {
            if (Uri.TryCreate(e.Uri, UriKind.Absolute, out var uri) && isAllowed(uri)) return;

            e.Cancel = true;
            Log.Information("Blocked frame navigation to {Uri}", e.Uri);
        };

        core.PermissionRequested += (_, e) =>
        {
            e.State = CoreWebView2PermissionState.Deny;
            Log.Information("Denied {Permission} for {Uri}", e.PermissionKind, e.Uri);
        };

        // carried over from Papercut.UI DisableEdgeFeaturesHelper
        core.Settings.IsZoomControlEnabled = true;
        core.Settings.AreDefaultScriptDialogsEnabled = false;
        core.Settings.IsBuiltInErrorPageEnabled = false;
        core.Settings.IsStatusBarEnabled = true;
        core.Settings.IsPasswordAutosaveEnabled = false;
        core.Settings.IsGeneralAutofillEnabled = false;

        Log.Information("WebView2 {Version} hooks attached", core.Environment.BrowserVersionString);
    }
}
