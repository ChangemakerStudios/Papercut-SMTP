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

/// <summary>
/// Per-user folders. Velopack replaces the exe folder on every update, so nothing is kept there.
/// </summary>
internal static class ShellPaths
{
    /// <summary>
    /// Windows: the v7 data folder (%AppData%\Changemaker Studios\Papercut SMTP), which already holds
    /// v7 messages and rules. macOS: ~/Library/Application Support/Papercut SMTP -- .NET maps
    /// ApplicationData to ~/.config there. PAPERCUT_DATA_DIR overrides both, for testing.
    /// </summary>
    public static string DataDirectory { get; } = ResolveDataDirectory();

    /// <summary>
    /// PAPERCUT_DATA_DIR is set: a self-contained test run -- no v7 import, messages stay in it.
    /// </summary>
    public static bool IsIsolated => !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("PAPERCUT_DATA_DIR"));

    public static string LogDirectory => Path.Combine(DataDirectory, "Logs");

    public static string WebViewDataDirectory => Path.Combine(DataDirectory, "WebView2");

    public static string SettingsFile => Path.Combine(DataDirectory, "Papercut.Shell.json");

    /// <summary>
    /// What v7 meant by %BaseDirectory%: the install root, never Velopack's "current" folder.
    /// </summary>
    public static string LegacyBaseDirectory
    {
        get
        {
            var baseDirectory = AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar);

            return baseDirectory.EndsWith("current", StringComparison.OrdinalIgnoreCase)
                ? Path.GetDirectoryName(baseDirectory)!
                : baseDirectory;
        }
    }

    private static string ResolveDataDirectory()
    {
        var overridePath = Environment.GetEnvironmentVariable("PAPERCUT_DATA_DIR");

        var path = !string.IsNullOrWhiteSpace(overridePath)
            ? Path.GetFullPath(overridePath)
            : OperatingSystem.IsMacOS()
                ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Papercut SMTP")
                : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Changemaker Studios", "Papercut SMTP");

        Directory.CreateDirectory(path);

        return path;
    }
}
