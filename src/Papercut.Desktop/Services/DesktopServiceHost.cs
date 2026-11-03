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
using System.Net;
using System.Net.Sockets;

namespace Papercut.Desktop.Services;

/// <summary>
/// Source #3: runs service/Papercut.Service as a child process. It gets an explicit url and data
/// folder, so nothing is guessed, and it exits when the shell closes its stdin or dies.
/// </summary>
internal sealed class DesktopServiceHost(ShellSettings settings) : IAsyncDisposable
{
    /// <summary>
    /// Fixed so the web UI's local storage (theme, sort) keeps the same origin across runs.
    /// </summary>
    public const int PreferredHttpPort = 37410;

    private static readonly TimeSpan ReadyTimeout = TimeSpan.FromSeconds(30);

    private static readonly TimeSpan StopTimeout = TimeSpan.FromSeconds(5);

    private readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(2) };

    private Process? _process;

    private bool _stopping;

    /// <summary>
    /// Raised when the child exits without being asked to.
    /// </summary>
    public event Action<int>? ExitedUnexpectedly;

    public Uri? BaseUri { get; private set; }

    public bool IsRunning => _process is { HasExited: false };

    public static string? FindServiceExecutable()
    {
        var exeName = OperatingSystem.IsWindows() ? "Papercut.Service.exe" : "Papercut.Service";

        var overridePath = Environment.GetEnvironmentVariable("PAPERCUT_SERVICE_EXE");
        if (!string.IsNullOrWhiteSpace(overridePath)) return File.Exists(overridePath) ? overridePath : null;

        // packaged layout: Papercut.exe + service/
        var packaged = Path.Combine(AppContext.BaseDirectory, "service", exeName);
        if (File.Exists(packaged)) return packaged;

#if DEBUG
        // dev: src/Papercut.Desktop/bin/Debug/<tfm>/ -> src/Papercut.Service/bin/Debug/net10.0/
        var dev = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..",
            "Papercut.Service", "bin", "Debug", "net10.0", exeName));
        if (File.Exists(dev)) return dev;
#endif

        return null;
    }

    public async Task<Uri> StartAsync(CancellationToken token)
    {
        if (IsRunning && BaseUri != null) return BaseUri;

        var executable = FindServiceExecutable()
                         ?? throw new FileNotFoundException("Papercut.Service was not found beside the shell (service/).");

        var port = PickHttpPort();
        var baseUri = new Uri($"http://127.0.0.1:{port}/");

        var startInfo = new ProcessStartInfo(executable)
        {
            WorkingDirectory = Path.GetDirectoryName(executable)!,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        foreach (var arg in BuildArguments(baseUri)) startInfo.ArgumentList.Add(arg);

        // the service's appsettings.Development.json would rebind it to a dev port
        startInfo.Environment["ASPNETCORE_ENVIRONMENT"] = "Production";
        startInfo.Environment["DOTNET_ENVIRONMENT"] = "Production";

        Log.Information("Starting the desktop service {Executable} at {BaseUri}", executable, baseUri);

        _stopping = false;

        var process = Process.Start(startInfo) ?? throw new InvalidOperationException($"Failed to start {executable}");

#if WINDOWS
        // if the shell crashes, Windows kills the child with the job
        Platform.Windows.ChildProcessJob.Assign(process);
#endif

        process.EnableRaisingEvents = true;
        process.Exited += (_, _) => OnExited(process);
        process.OutputDataReceived += (_, e) => LogOutput(e.Data);
        process.ErrorDataReceived += (_, e) => LogOutput(e.Data);
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        _process = process;
        BaseUri = baseUri;

        await WaitUntilReadyAsync(process, baseUri, token);

        Log.Information("Desktop service is ready at {BaseUri}", baseUri);

        return baseUri;
    }

    public async Task StopAsync()
    {
        var process = _process;

        if (process == null) return;

        _stopping = true;

        try
        {
            if (!process.HasExited)
            {
                Log.Information("Stopping the desktop service");

                // graceful: the service's ParentProcessWatcher stops when stdin closes
                process.StandardInput.Close();

                using var timeout = new CancellationTokenSource(StopTimeout);

                try
                {
                    await process.WaitForExitAsync(timeout.Token);
                }
                catch (OperationCanceledException)
                {
                    Log.Warning("Desktop service did not stop within {Timeout}s -- killing it", StopTimeout.TotalSeconds);
                    process.Kill(entireProcessTree: true);
                }
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Error stopping the desktop service");
        }
        finally
        {
            process.Dispose();
            _process = null;
            BaseUri = null;
        }
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync();
        _http.Dispose();
    }

    private IEnumerable<string> BuildArguments(Uri baseUri)
    {
        yield return $"--urls={baseUri.GetLeftPart(UriPartial.Authority)}";
        yield return $"--DataDirectory={ShellPaths.DataDirectory}";
        yield return $"--ParentProcessId={Environment.ProcessId}";
        yield return $"--HttpPathPrefix=";
        yield return $"--SmtpServer:MessagePath={WithLegacyBaseDirectory(settings.MessagePaths)}";
        yield return $"--SmtpServer:LoggingPath={WithLegacyBaseDirectory(settings.LoggingPaths)}";

        // defaults only: SMTP IP/Port saved in the web UI's Options (or imported from v7) win.
        // macOS: ports below 1024 need root.
        yield return "--SmtpServer:IP=Any";
        yield return $"--SmtpServer:Port={(OperatingSystem.IsWindows() ? 25 : 2525)}";
    }

    /// <summary>
    /// The child's %BaseDirectory% is service/ inside Velopack's current/, which updates wipe.
    /// v7 meant the install root, so pin it there.
    /// </summary>
    private static string WithLegacyBaseDirectory(string paths) =>
        paths.Replace("%BaseDirectory%", ShellPaths.LegacyBaseDirectory, StringComparison.OrdinalIgnoreCase);

    private async Task WaitUntilReadyAsync(Process process, Uri baseUri, CancellationToken token)
    {
        var deadline = DateTime.UtcNow + ReadyTimeout;
        var health = new Uri(baseUri, "health");

        while (DateTime.UtcNow < deadline)
        {
            token.ThrowIfCancellationRequested();

            if (process.HasExited)
                throw new InvalidOperationException($"The desktop service exited during startup (code {process.ExitCode}). See the service log.");

            try
            {
                using var response = await _http.GetAsync(health, token);
                if (response.IsSuccessStatusCode) return;
            }
            catch (HttpRequestException)
            {
            }
            catch (TaskCanceledException) when (!token.IsCancellationRequested)
            {
            }

            await Task.Delay(250, token);
        }

        throw new TimeoutException($"The desktop service did not answer at {health} within {ReadyTimeout.TotalSeconds}s.");
    }

    private void OnExited(Process process)
    {
        if (_stopping || !ReferenceEquals(process, _process)) return;

        var code = SafeExitCode(process);

        Log.Warning("Desktop service exited unexpectedly with code {ExitCode}", code);

        ExitedUnexpectedly?.Invoke(code);
    }

    private static int SafeExitCode(Process process)
    {
        try
        {
            return process.ExitCode;
        }
        catch
        {
            return -1;
        }
    }

    private static void LogOutput(string? line)
    {
        // the service writes its own log file; this is just for the shell's diagnostics
        if (!string.IsNullOrWhiteSpace(line)) Log.Debug("[service] {Line}", line);
    }

    private static int PickHttpPort()
    {
        if (IsPortFree(PreferredHttpPort)) return PreferredHttpPort;

        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();

        Log.Warning("Port {PreferredPort} is in use -- the desktop service will use {Port}", PreferredHttpPort, port);

        return port;
    }

    private static bool IsPortFree(int port)
    {
        try
        {
            using var listener = new TcpListener(IPAddress.Loopback, port);
            listener.Start();
            listener.Stop();
            return true;
        }
        catch (SocketException)
        {
            return false;
        }
    }
}
