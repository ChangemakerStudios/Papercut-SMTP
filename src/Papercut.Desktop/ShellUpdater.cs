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


using Velopack;
using Velopack.Sources;

namespace Papercut.Desktop;

/// <summary>
/// Velopack updates from GitHub releases, on the channel this build was packed with
/// (win-x64-stable, win-x64-dev, ...). Updates download in the background; the user
/// restarts from the tray, or they are applied when Papercut exits.
/// </summary>
internal sealed class ShellUpdater
{
    private const string RepositoryUrl = "https://github.com/ChangemakerStudios/Papercut-SMTP";

    private static readonly TimeSpan CheckInterval = TimeSpan.FromHours(12);

    private readonly UpdateManager? _manager;

    public ShellUpdater()
    {
        try
        {
            // prerelease: true lets -dev channels see their builds; a stable channel's
            // releases.<channel>.json only exists on stable releases, so it is unaffected
            _manager = new UpdateManager(new GithubSource(RepositoryUrl, null, true));
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Update manager unavailable");
        }
    }

    public event Action? UpdateReady;

    public bool IsInstalled => _manager?.IsInstalled == true;

    public string? CurrentVersion => _manager?.CurrentVersion?.ToString();

    public VelopackAsset? PendingUpdate { get; private set; }

    public void StartChecking(CancellationToken token)
    {
        if (!IsInstalled)
        {
            Log.Information("Not a Velopack install -- update checks are off");
            return;
        }

        _ = Task.Run(
            async () =>
            {
                await Task.Delay(TimeSpan.FromSeconds(30), token);

                while (!token.IsCancellationRequested)
                {
                    await CheckAsync();
                    await Task.Delay(CheckInterval, token);
                }
            },
            token);
    }

    public async Task CheckAsync()
    {
        if (_manager == null || !IsInstalled || PendingUpdate != null) return;

        try
        {
            var update = await _manager.CheckForUpdatesAsync();

            if (update == null)
            {
                Log.Information("Papercut {Version} is up to date", CurrentVersion);
                return;
            }

            Log.Information("Downloading update {Version}", update.TargetFullRelease.Version);

            await _manager.DownloadUpdatesAsync(update);

            PendingUpdate = update.TargetFullRelease;
            UpdateReady?.Invoke();
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Update check failed");
        }
    }

    /// <summary>
    /// Restarts into the downloaded update. The caller stops the desktop service first.
    /// </summary>
    public void ApplyAndRestart()
    {
        if (_manager == null || PendingUpdate == null) return;

        Log.Information("Restarting to apply update {Version}", PendingUpdate.Version);
        _manager.ApplyUpdatesAndRestart(PendingUpdate);
    }

    /// <summary>
    /// On a normal exit, applies a downloaded update after the process ends.
    /// </summary>
    public void ApplyOnExit()
    {
        if (_manager == null || PendingUpdate == null) return;

        try
        {
            _manager.WaitExitThenApplyUpdates(PendingUpdate, silent: true, restart: false);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Unable to schedule the update for exit");
        }
    }
}
