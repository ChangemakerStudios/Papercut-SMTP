// Papercut
// 
// Copyright © 2008 - 2012 Ken Robertson
// Copyright © 2013 - 2025 Jaben Cargman
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


using NuGet.Versioning;

using Papercut.Core.Infrastructure.Container;

using Velopack;

using ILogger = Serilog.ILogger;

namespace Papercut.AppLayer.NewVersionCheck;

public class NewVersionCheckHandler(UpdateManager updateManager, ILogger logger) : Disposable, IAppLifecycleStarted, INewVersionProvider
{
    private readonly CancellationTokenSource _cancellationTokenSource = new();

    private readonly TaskCompletionSource<UpdateInfo?> _updateTask = new();

    private Task? _backgroundTask;

    public Task OnStartedAsync()
    {
        _backgroundTask = Task.Run(RunNewVersionCheck, _cancellationTokenSource.Token);

        return _backgroundTask.IsCompleted ? _backgroundTask : Task.CompletedTask;
    }

    public async Task<UpdateInfo?> GetLatestVersionAsync(CancellationToken token = default)
    {
        return await _updateTask.Task;
    }

    protected override async ValueTask DisposeAsync(bool disposing)
    {
        if (disposing)
        {
            if (_backgroundTask != null)
            {
                await _cancellationTokenSource.CancelAsync();

                try
                {
                    await _backgroundTask;
                }
                catch (Exception)
                {
                    // nothing
                }
            }

            _updateTask.SetCanceled();

            try
            {
                await _updateTask.Task;
            }
            catch (Exception)
            {
                // nothing
            }
        }
    }

    private async Task RunNewVersionCheck()
    {
        if (updateManager.IsInstalled)
        {
            try
            {
                // check for new version
                var newVersion = await updateManager.CheckForUpdatesAsync();
                if (newVersion != null)
                {
                    logger.Information("New Version of Papercut SMTP is Available {@NewVersion}", newVersion);

                    _updateTask.SetResult(newVersion);
                }
            }
            catch (Exception ex)
            {
                _updateTask.SetException(ex);
            }
        }
        else
        {
            logger.Debug("Papercut was not installed via Velopack. Cannot check for new versions.");
        }

        this._updateTask.SetResult(null);

        // for testing
        //_updateTask.SetResult(new UpdateInfo(new VelopackAsset() { Version = new SemanticVersion(10, 0, 0) }, false));
    }

    #region Begin Static Container Registrations

    static void Register(ContainerBuilder builder)
    {
        builder.RegisterType<NewVersionCheckHandler>().As<IAppLifecycleStarted>().As<INewVersionProvider>().InstancePerUIScope();
    }

    #endregion
}