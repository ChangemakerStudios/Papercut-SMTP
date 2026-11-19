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


using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security;

namespace Papercut.Desktop.Platform.MacOS;

/// <summary>
/// The Dock icon follows the window: shown while it is open, gone while Papercut lives only
/// in the menu bar. Avalonia only sets this at startup (MacOSPlatformOptions.ShowInDock), so
/// this flips NSApp's activation policy directly. Main thread only.
/// </summary>
internal static class MacDock
{
    private const string ObjC = "/usr/lib/libobjc.A.dylib";

    private const long Regular = 0;

    private const long Accessory = 1;

    public static void SetVisible(bool visible) =>
        Send("change the Dock icon", app => objc_msgSend(app, sel_registerName("setActivationPolicy:"), visible ? Regular : Accessory));

    /// <summary>
    /// The app menu's Hide (Cmd+H): [NSApp hide:nil].
    /// </summary>
    public static void HideApp() =>
        Send("hide the app", app => objc_msgSend(app, sel_registerName("hide:"), IntPtr.Zero));

    private static void Send(string what, Action<IntPtr> send)
    {
        if (!OperatingSystem.IsMacOS()) return;

        try
        {
            var app = objc_msgSend(objc_getClass("NSApplication"), sel_registerName("sharedApplication"));
            if (app != IntPtr.Zero) send(app);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to {What}", what);
        }
    }

    [DllImport(ObjC)]
    private static extern IntPtr objc_getClass(string name);

    [DllImport(ObjC)]
    private static extern IntPtr sel_registerName(string name);

    [DllImport(ObjC)]
    private static extern IntPtr objc_msgSend(IntPtr receiver, IntPtr selector);

    [DllImport(ObjC)]
    [return: MarshalAs(UnmanagedType.I1)]
    private static extern bool objc_msgSend(IntPtr receiver, IntPtr selector, long value);

    [DllImport(ObjC)]
    private static extern void objc_msgSend(IntPtr receiver, IntPtr selector, IntPtr arg);
}

/// <summary>
/// Interim: osascript's "display notification" works unsigned, but is attributed to Script
/// Editor and cannot report clicks. UserNotifications needs the packaged .app (bundle id).
/// </summary>
internal sealed class MacNotifier : INotifier
{
    public void Show(string title, string text, Action? onClick)
    {
        try
        {
            var script = $"display notification {Quote(text)} with title \"Papercut SMTP\" subtitle {Quote(title)}";

            var startInfo = new ProcessStartInfo("osascript") { UseShellExecute = false, CreateNoWindow = true };
            startInfo.ArgumentList.Add("-e");
            startInfo.ArgumentList.Add(script);

            Process.Start(startInfo)?.Dispose();
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to show a notification");
        }
    }

    private static string Quote(string value) =>
        "\"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace('\n', ' ').Replace('\r', ' ') + "\"";
}

/// <summary>
/// ~/Library/LaunchAgents plist with RunAtLoad.
/// </summary>
internal sealed class MacLaunchAgent : IRunAtStartup
{
    private const string Label = "com.changemakerstudios.papercutsmtp";

    private static string PlistPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Library", "LaunchAgents", $"{Label}.plist");

    public bool IsEnabled => File.Exists(PlistPath);

    public void Set(bool enabled)
    {
        if (!enabled)
        {
            File.Delete(PlistPath);
            return;
        }

        var executable = Environment.ProcessPath ?? throw new InvalidOperationException("No process path");

        Directory.CreateDirectory(Path.GetDirectoryName(PlistPath)!);
        File.WriteAllText(
            PlistPath,
            $"""
             <?xml version="1.0" encoding="UTF-8"?>
             <!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
             <plist version="1.0">
             <dict>
               <key>Label</key>
               <string>{Label}</string>
               <key>ProgramArguments</key>
               <array>
                 <string>{SecurityElement.Escape(executable)}</string>
               </array>
               <key>RunAtLoad</key>
               <true/>
             </dict>
             </plist>
             """);
    }
}
