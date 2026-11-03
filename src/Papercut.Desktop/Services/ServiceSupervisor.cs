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


namespace Papercut.Desktop.Services;

internal enum ServiceSourceKind
{
    None,
    External,
    WindowsService,
    Desktop
}

/// <summary>
/// Decides which Papercut service the shell uses, first match wins:
/// 1. one already reachable (PAPERCUT_SERVICE_URL, the configured external url, Docker on :8080)
/// 2. the installed Windows Service, when it is running
/// 3. the desktop service, started as a child
/// Only one Papercut owns the SMTP port, so the child only starts when 1 and 2 are not there.
/// </summary>
internal sealed class ServiceSupervisor : IAsyncDisposable
{
    private const int MaxRestarts = 5;

    private static readonly TimeSpan RestartWindow = TimeSpan.FromMinutes(2);

    // 127.0.0.1, not localhost: a refused localhost connect on Windows retries IPv6 then IPv4 (~2s)
    private static readonly Uri DockerDefault = new("http://127.0.0.1:8080/");

    private readonly ShellSettings _settings;

    private readonly DesktopServiceHost _desktop;

    private readonly IWindowsServiceControl? _windowsService;

    private readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(2) };

    private readonly SemaphoreSlim _gate = new(1, 1);

    private readonly Queue<DateTime> _restarts = new();

    private int _failedChecks;

    public ServiceSupervisor(ShellSettings settings, DesktopServiceHost desktop, IWindowsServiceControl? windowsService)
    {
        _settings = settings;
        _desktop = desktop;
        _windowsService = windowsService;
        _desktop.ExitedUnexpectedly += exitCode => _ = RestartDesktopAfterCrashAsync();
    }

    /// <summary>
    /// Raised (on a pool thread) whenever the source, url or status changes.
    /// </summary>
    public event Action? Changed;

    public ServiceSourceKind Kind { get; private set; }

    public Uri? BaseUri { get; private set; }

    public bool IsStarting { get; private set; }

    public string? LastError { get; private set; }

    public bool IsWindowsServiceInstalled => _windowsService?.IsInstalled == true;

    public bool IsWindowsServiceRunning => _windowsService?.IsRunning == true;

    public string StatusText =>
        IsStarting ? "Starting..."
        : Kind switch
        {
            ServiceSourceKind.Desktop when IsWindowsServiceInstalled => "Running (Windows Service stopped)",
            ServiceSourceKind.Desktop => "Running",
            ServiceSourceKind.WindowsService => "Running (Windows Service)",
            ServiceSourceKind.External => $"Connected to {BaseUri?.Authority}",
            _ => LastError == null ? "Not running" : "Not running -- see Logs"
        };

    public async Task ResolveAsync(CancellationToken token = default)
    {
        await _gate.WaitAsync(token);

        try
        {
            await ResolveCoreAsync(token);
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>
    /// Called on a timer. A connect-only source that stops answering is re-resolved, which
    /// starts the desktop service if nothing else is there.
    /// </summary>
    public async Task CheckAsync(CancellationToken token = default)
    {
        if (Kind is not (ServiceSourceKind.External or ServiceSourceKind.WindowsService) || BaseUri == null) return;

        if (await IsPapercutAsync(BaseUri, token))
        {
            _failedChecks = 0;
            return;
        }

        if (++_failedChecks < 3) return;

        Log.Warning("{Kind} service at {BaseUri} stopped answering -- resolving again", Kind, BaseUri);

        _failedChecks = 0;
        await ResolveAsync(token);
    }

    /// <summary>
    /// Stops the desktop service first, so the two do not fight over the SMTP port.
    /// </summary>
    public async Task StartWindowsServiceAsync()
    {
        if (_windowsService == null) return;

        await _gate.WaitAsync();

        try
        {
            await _desktop.StopAsync();
            SetState(ServiceSourceKind.None, null, starting: true);

            await _windowsService.StartAsync();

            // Running comes before Kestrel answers
            if (_windowsService.GetBaseUri() is { } windowsUri)
            {
                for (var i = 0; i < 30 && !await IsPapercutAsync(windowsUri, CancellationToken.None); i++)
                    await Task.Delay(500);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to start the Windows Service");
            LastError = ex.Message;
        }
        finally
        {
            await ResolveCoreAsync(CancellationToken.None);
            _gate.Release();
        }
    }

    public async Task StopWindowsServiceAsync()
    {
        if (_windowsService == null) return;

        await _gate.WaitAsync();

        try
        {
            SetState(ServiceSourceKind.None, null, starting: true);
            await _windowsService.StopAsync();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to stop the Windows Service");
            LastError = ex.Message;
        }
        finally
        {
            await ResolveCoreAsync(CancellationToken.None);
            _gate.Release();
        }
    }

    /// <summary>
    /// Before a Velopack update: the child holds files in current\.
    /// </summary>
    public Task StopDesktopAsync() => _desktop.StopAsync();

    public async ValueTask DisposeAsync()
    {
        await _desktop.DisposeAsync();
        _http.Dispose();
    }

    private async Task ResolveCoreAsync(CancellationToken token)
    {
        SetState(Kind, BaseUri, starting: true);

        try
        {
            if (_windowsService?.IsRunning == true
                && _windowsService.GetBaseUri() is { } windowsUri
                && await IsPapercutAsync(windowsUri, token))
            {
                await _desktop.StopAsync();
                LastError = null;
                SetState(ServiceSourceKind.WindowsService, windowsUri, starting: false);
                return;
            }

            foreach (var candidate in ExternalCandidates())
            {
                if (candidate == _desktop.BaseUri || !await IsPapercutAsync(candidate, token)) continue;

                await _desktop.StopAsync();
                LastError = null;
                SetState(ServiceSourceKind.External, candidate, starting: false);
                return;
            }

            var desktopUri = await _desktop.StartAsync(token);
            LastError = null;
            SetState(ServiceSourceKind.Desktop, desktopUri, starting: false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            Log.Error(ex, "No Papercut service is available");
            LastError = ex.Message;
            SetState(ServiceSourceKind.None, null, starting: false);
        }
    }

    private IEnumerable<Uri> ExternalCandidates()
    {
        var configured = new[] { Environment.GetEnvironmentVariable("PAPERCUT_SERVICE_URL"), _settings.ExternalServiceUrl }
            .Select(ServiceUrls.Normalize)
            .OfType<Uri>();

        return configured.Append(DockerDefault).Distinct();
    }

    private async Task<bool> IsPapercutAsync(Uri baseUri, CancellationToken token)
    {
        try
        {
            using var response = await _http.GetAsync(new Uri(baseUri, "health"), token);

            // anything can sit on :8080 -- only connect to a Papercut
            return response.IsSuccessStatusCode
                   && (await response.Content.ReadAsStringAsync(token)).Contains("Papercut", StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException && !token.IsCancellationRequested)
        {
            return false;
        }
    }

    private async Task RestartDesktopAfterCrashAsync()
    {
        var now = DateTime.UtcNow;

        while (_restarts.Count > 0 && now - _restarts.Peek() > RestartWindow) _restarts.Dequeue();

        if (_restarts.Count >= MaxRestarts)
        {
            Log.Error("Desktop service crashed {Count} times in {Window} -- not restarting it", MaxRestarts, RestartWindow);
            LastError = "The service keeps crashing";
            await _desktop.StopAsync();
            SetState(ServiceSourceKind.None, null, starting: false);
            return;
        }

        _restarts.Enqueue(now);

        SetState(ServiceSourceKind.None, null, starting: true);
        await _desktop.StopAsync();
        await Task.Delay(TimeSpan.FromSeconds(2));
        await ResolveAsync();
    }

    private void SetState(ServiceSourceKind kind, Uri? baseUri, bool starting)
    {
        var changed = Kind != kind || BaseUri != baseUri || IsStarting != starting;

        Kind = kind;
        BaseUri = baseUri;
        IsStarting = starting;

        if (!changed) return;

        Log.Information("Service source: {Kind} {BaseUri} {Status}", kind, baseUri, StatusText);
        Changed?.Invoke();
    }
}
