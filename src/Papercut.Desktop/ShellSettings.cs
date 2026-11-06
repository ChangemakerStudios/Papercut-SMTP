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


using System.Text.Json;

namespace Papercut.Desktop;

/// <summary>
/// Shell-only settings. SMTP and server settings belong to the service and are edited in the web UI.
/// </summary>
internal sealed class ShellSettings
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public bool ShowNotifications { get; set; } = true;

    public int WindowWidth { get; set; } = 1200;

    public int WindowHeight { get; set; } = 800;

    /// <summary>
    /// Message folders for the desktop service, in the service's ';'-separated template form.
    /// Defaults to the v7 list so v7 messages show up.
    /// </summary>
    public string MessagePaths { get; set; } = DefaultPaths("Incoming");

    /// <summary>
    /// The desktop service logs beside the shell, so "View Logs" is one folder.
    /// </summary>
    public string LoggingPaths { get; set; } = ShellPaths.LogDirectory;

    /// <summary>
    /// A Papercut service to use instead of starting one, e.g. Docker at http://localhost:8080.
    /// </summary>
    public string? ExternalServiceUrl { get; set; }

    /// <summary>
    /// Set once the v7 settings import has run, so it never runs twice.
    /// </summary>
    public bool V7MigrationDone { get; set; }

    /// <summary>
    /// Set by the v7 import when rules couldn't come across; cleared once the notice has shown.
    /// </summary>
    public bool ShowV7RulesNotice { get; set; }

    public static ShellSettings Load()
    {
        try
        {
            if (File.Exists(ShellPaths.SettingsFile))
            {
                return JsonSerializer.Deserialize<ShellSettings>(File.ReadAllText(ShellPaths.SettingsFile), JsonOptions)
                       ?? new ShellSettings();
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Unable to read {SettingsFile} -- using defaults", ShellPaths.SettingsFile);
        }

        return new ShellSettings();
    }

    public void Save()
    {
        try
        {
            File.WriteAllText(ShellPaths.SettingsFile, JsonSerializer.Serialize(this, JsonOptions));
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Unable to save {SettingsFile}", ShellPaths.SettingsFile);
        }
    }

    // the v7 defaults (Papercut.UI Settings.settings); macOS has no v7 data, so only the data folder
    private static string DefaultPaths(string subFolder) =>
        OperatingSystem.IsWindows() && !ShellPaths.IsIsolated
            ? $@"%ApplicationData%\Changemaker Studios\Papercut SMTP;%ApplicationData%\Papercut;%BaseDirectory%\{subFolder};%AppDataDirectory%\{subFolder}"
            : ShellPaths.DataDirectory;
}
