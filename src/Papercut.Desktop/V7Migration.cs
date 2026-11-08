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
using System.Text.Json.Nodes;
using System.Xml.Linq;


namespace Papercut.Desktop;

/// <summary>
/// First run after the update from the v7 WPF app. Runs once (ShellSettings.V7MigrationDone).
/// Messages need nothing: the desktop service reads the same folders. This brings over the WPF
/// user.config settings. v7's rules.json is gone by now: it lived in current\, which Velopack
/// replaces before any v8 code runs. Only a Windows Service's copy can be recovered.
/// </summary>
internal static class V7Migration
{
    /// <summary>
    /// The desktop service's settings file (ServiceSettingStore under DataDirectory).
    /// </summary>
    public static string ServiceSettingsFile => Path.Combine(ShellPaths.DataDirectory, "Papercut.SMTP.Service.Settings.json");

    private static string RulesFile => Path.Combine(ShellPaths.DataDirectory, "rules.json");

    public static void Run(ShellSettings settings)
    {
        if (settings.V7MigrationDone || ShellPaths.IsIsolated) return;

        try
        {
            if (OperatingSystem.IsWindows())
            {
                var foundV7Settings = ImportUserConfig(settings);
                ImportWindowsServiceRules();
                settings.ShowV7RulesNotice = RulesNeedRecreating(foundV7Settings, RulesFile);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "v7 settings import failed -- continuing with defaults");
        }

        settings.V7MigrationDone = true;
        settings.Save();
    }

    /// <summary>
    /// Can't tell whether v7 had any rules, so anyone updating from v7 without a copy gets told.
    /// </summary>
    internal static bool RulesNeedRecreating(bool foundV7Settings, string rulesFile) =>
        foundV7Settings && !File.Exists(rulesFile);

    private static bool ImportUserConfig(ShellSettings settings)
    {
        var userConfig = FindNewestUserConfig();

        if (userConfig == null)
        {
            Log.Information("No v7 user.config found -- nothing to import");
            return false;
        }

        Log.Information("Importing v7 settings from {UserConfig}", userConfig);

        var values = ReadSettings(userConfig);

        // SMTP: the service defaults to 2525, v7 to 25, so it must carry over
        var service = new Dictionary<string, string>();
        if (values.TryGetValue("IP", out var ip) && !string.IsNullOrWhiteSpace(ip)) service["IP"] = ip;
        if (values.TryGetValue("Port", out var port) && int.TryParse(port, out _)) service["Port"] = port;
        MergeServiceSettings(service);

        if (values.TryGetValue("MessagePaths", out var messagePaths) && !string.IsNullOrWhiteSpace(messagePaths))
            settings.MessagePaths = messagePaths;

        if (values.TryGetValue("ShowNotifications", out var notifications) && bool.TryParse(notifications, out var show))
            settings.ShowNotifications = show;

        if (values.TryGetValue("MainWindowWidth", out var width) && int.TryParse(width, out var w) && w >= 640)
            settings.WindowWidth = w;

        if (values.TryGetValue("MainWindowHeight", out var height) && int.TryParse(height, out var h) && h >= 480)
            settings.WindowHeight = h;

        // RunOnStartup: v8 uses the same HKCU Run value and exe path, so it carries over as is.
        // LoggingPaths: logs are not user data; the desktop service logs to the shell's Logs folder.
        // Forward*, theme, sort order, zoom: no shell or service equivalent yet -- noted in the release notes.
        Log.Information(
            "Imported v7 settings: SMTP {Ip}:{Port}, message paths {MessagePaths}",
            service.GetValueOrDefault("IP", "(default)"),
            service.GetValueOrDefault("Port", "(default)"),
            settings.MessagePaths);

        return true;
    }

    /// <summary>
    /// %LocalAppData%\Changemaker_Studios\Papercut.exe_Url_*\&lt;version&gt;\user.config. There is one
    /// folder per exe path that ever ran; the newest file is the install the user last used.
    /// </summary>
    private static string? FindNewestUserConfig()
    {
        var root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Changemaker_Studios");

        if (!Directory.Exists(root)) return null;

        return Directory.EnumerateDirectories(root, "Papercut*")
            .SelectMany(d => Directory.EnumerateFiles(d, "user.config", SearchOption.AllDirectories))
            .OrderByDescending(File.GetLastWriteTimeUtc)
            .FirstOrDefault();
    }

    internal static Dictionary<string, string> ReadSettings(string userConfigPath)
    {
        var document = XDocument.Load(userConfigPath);

        return document.Root?
                   .Element("userSettings")?
                   .Element("Papercut.Properties.Settings")?
                   .Elements("setting")
                   .Where(s => s.Attribute("name") != null)
                   .GroupBy(s => s.Attribute("name")!.Value)
                   .ToDictionary(g => g.Key, g => g.First().Element("value")?.Value ?? string.Empty)
               ?? new Dictionary<string, string>();
    }

    /// <summary>
    /// Adds keys the service settings file does not have yet; never overwrites one.
    /// </summary>
    private static void MergeServiceSettings(Dictionary<string, string> values)
    {
        if (values.Count == 0) return;

        var json = File.Exists(ServiceSettingsFile)
            ? JsonNode.Parse(File.ReadAllText(ServiceSettingsFile)) as JsonObject ?? new JsonObject()
            : new JsonObject();

        foreach (var (key, value) in values)
        {
            if (!json.ContainsKey(key)) json[key] = value;
        }

        File.WriteAllText(ServiceSettingsFile, json.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
    }

    /// <summary>
    /// When v7 ran alongside the Windows Service, the WPF app synced its rules to the service.
    /// If the data folder has none, take that copy.
    /// </summary>
    private static void ImportWindowsServiceRules()
    {
        var target = RulesFile;

        if (File.Exists(target)) return;

        var serviceDirectory = WindowsServiceDirectory();
        var source = serviceDirectory == null ? null : Path.Combine(serviceDirectory, "rules.json");

        if (source == null || !File.Exists(source)) return;

        File.Copy(source, target);
        Log.Information("Imported rules from the Windows Service: {Source}", source);
    }

    private static string? WindowsServiceDirectory()
    {
#if WINDOWS
        using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\Papercut.SMTP.Service");

        if (key?.GetValue("ImagePath") is not string imagePath) return null;

        imagePath = imagePath.Trim();
        var exe = imagePath.StartsWith('"') ? imagePath[1..Math.Max(1, imagePath.IndexOf('"', 1))] : imagePath;

        return Path.GetDirectoryName(exe);
#else
        return null;
#endif
    }
}
