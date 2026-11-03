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


using System.ComponentModel;
using System.Diagnostics;
using System.ServiceProcess;
using System.Text.Json;

using Microsoft.Win32;

using Papercut.Desktop.Services;

namespace Papercut.Desktop.Platform.Windows;

/// <summary>
/// Source #2: the Papercut Windows Service from the service zip. The shell connects to it when it
/// runs, and can start/stop it -- elevating through sc.exe when the shell is not an administrator.
/// </summary>
internal sealed class WindowsServiceControl : IWindowsServiceControl
{
    private const string ServiceName = "Papercut.SMTP.Service";

    private static readonly TimeSpan StatusTimeout = TimeSpan.FromSeconds(30);

    public bool IsInstalled => GetStatus() != null;

    public bool IsRunning => GetStatus() == ServiceControllerStatus.Running;

    /// <summary>
    /// From the "Urls" in the installed service's appsettings.json, via the registry ImagePath.
    /// </summary>
    public Uri? GetBaseUri()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey($@"SYSTEM\CurrentControlSet\Services\{ServiceName}");

            if (key?.GetValue("ImagePath") is not string imagePath || string.IsNullOrWhiteSpace(imagePath)) return null;

            var directory = Path.GetDirectoryName(UnquoteImagePath(imagePath));
            var settingsPath = directory == null ? null : Path.Combine(directory, "appsettings.json");

            if (settingsPath == null || !File.Exists(settingsPath)) return ServiceUrls.Normalize("http://localhost:8080");

            using var document = JsonDocument.Parse(File.ReadAllText(settingsPath));

            var urls = document.RootElement.TryGetProperty("Urls", out var value) ? value.GetString() : null;

            return ServiceUrls.Normalize(urls ?? "http://localhost:8080");
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "Unable to read the Windows Service url");
            return null;
        }
    }

    public Task StartAsync() => ControlAsync("start", ServiceControllerStatus.Running, s => s.Start());

    public Task StopAsync() => ControlAsync("stop", ServiceControllerStatus.Stopped, s => s.Stop());

    private static async Task ControlAsync(string verb, ServiceControllerStatus target, Action<ServiceController> action)
    {
        using var service = new ServiceController(ServiceName);

        try
        {
            action(service);
        }
        catch (InvalidOperationException ex) when (ex.InnerException is Win32Exception { NativeErrorCode: 5 })
        {
            // access denied: ask for elevation once, for this one command
            Log.Information("Elevating to {Verb} the Windows Service", verb);

            using var elevated = Process.Start(new ProcessStartInfo("sc.exe", $"{verb} {ServiceName}")
            {
                UseShellExecute = true,
                Verb = "runas",
                WindowStyle = ProcessWindowStyle.Hidden
            });

            if (elevated != null) await elevated.WaitForExitAsync();
        }

        await Task.Run(() => service.WaitForStatus(target, StatusTimeout));
    }

    private static ServiceControllerStatus? GetStatus()
    {
        try
        {
            using var service = new ServiceController(ServiceName);
            return service.Status;
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }

    private static string UnquoteImagePath(string imagePath)
    {
        imagePath = imagePath.Trim();

        if (!imagePath.StartsWith('"')) return imagePath;

        var closing = imagePath.IndexOf('"', 1);

        return closing > 0 ? imagePath[1..closing] : imagePath.Trim('"');
    }
}
