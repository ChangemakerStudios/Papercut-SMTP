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


using Microsoft.Win32;

namespace Papercut.Desktop.Platform.Windows;

/// <summary>
/// HKCU Run value. Same name and target (current\Papercut.exe) as v7, so a v7 user who had
/// run-at-startup on keeps it through the update without anything being rewritten.
/// </summary>
internal sealed class WindowsRunAtStartup : IRunAtStartup
{
    private const string RunKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";

    private const string ValueName = "Papercut.Smtp.App";

    public bool IsEnabled
    {
        get
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKey, false);

            return string.Equals(key?.GetValue(ValueName) as string, Environment.ProcessPath, StringComparison.OrdinalIgnoreCase);
        }
    }

    public void Set(bool enabled)
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKey, true)
                        ?? throw new InvalidOperationException($"Cannot open HKCU\\{RunKey}");

        if (enabled)
        {
            key.SetValue(ValueName, Environment.ProcessPath ?? throw new InvalidOperationException("No process path"));
        }
        else
        {
            key.DeleteValue(ValueName, false);
        }

        Log.Information("Run at startup {State}", enabled ? "enabled" : "disabled");
    }
}
