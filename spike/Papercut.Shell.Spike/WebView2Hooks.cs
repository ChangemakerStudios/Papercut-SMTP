using System.Runtime.Versioning;

using Avalonia.Controls;
using Avalonia.Platform;

using Microsoft.Web.WebView2.Core;

namespace Papercut.Shell.Spike;

/// <summary>
/// Windows only: Avalonia's WebView2 adapter wires NavigationStarting (main frame only), so iframe
/// navigations and permission prompts are hooked on the raw CoreWebView2 it exposes.
/// </summary>
[SupportedOSPlatform("windows")]
internal static class WebView2Hooks
{
    public static void Attach(NativeWebView webView, Func<Uri, bool> isAllowed)
    {
        if (webView.TryGetPlatformHandle() is not IWindowsWebView2PlatformHandle handle || handle.CoreWebView2 == IntPtr.Zero)
        {
            SpikeLog.Write("WebView2Hooks", "MISSING", "no IWindowsWebView2PlatformHandle -- iframe navigations are NOT covered");
            return;
        }

        CoreWebView2 core;

        try
        {
            core = CoreWebView2.CreateFromComICoreWebView2(handle.CoreWebView2);
        }
        catch (Exception ex)
        {
            SpikeLog.Write("WebView2Hooks", "FAILED", $"CreateFromComICoreWebView2: {ex.GetType().Name}: {ex.Message}");
            return;
        }

        core.FrameNavigationStarting += (_, e) =>
        {
            var allowed = Uri.TryCreate(e.Uri, UriKind.Absolute, out var uri) && isAllowed(uri);
            e.Cancel = !allowed;

            SpikeLog.Write("FrameNavigationStarting", allowed ? "allowed" : "CANCELLED", e.Uri);
        };

        core.PermissionRequested += (_, e) =>
        {
            e.State = CoreWebView2PermissionState.Deny;
            SpikeLog.Write("PermissionRequested", "DENIED", $"{e.PermissionKind} {e.Uri}");
        };

        // carried over from Papercut.UI DisableEdgeFeaturesHelper
        core.Settings.IsZoomControlEnabled = true;
        core.Settings.AreDefaultScriptDialogsEnabled = false;
        core.Settings.IsBuiltInErrorPageEnabled = false;
        core.Settings.IsStatusBarEnabled = true;

        SpikeLog.Write("WebView2Hooks", "attached", $"FrameNavigationStarting + PermissionRequested on {core.Environment.BrowserVersionString}");
    }
}
