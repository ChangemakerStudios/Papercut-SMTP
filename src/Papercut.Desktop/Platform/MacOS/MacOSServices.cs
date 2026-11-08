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
using System.Security;

namespace Papercut.Desktop.Platform.MacOS;

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
