// Papercut
// 
// Copyright © 2008 - 2012 Ken Robertson
// Copyright © 2013 - 2024 Jaben Cargman
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


using Autofac;
using Autofac.Util;

using Microsoft.Extensions.Logging;

using NuGet.Versioning;

using Papercut.Core.Infrastructure.Container;
using Papercut.Domain.LifecycleHooks;

using Velopack;

namespace Papercut.AppLayer.NewVersionCheck;

public class NewVersionCheckHandler(ILogger<NewVersionCheckHandler> logger) : Disposable, IAppLifecycleStarted, INewVersionProvider
{
    private readonly CancellationTokenSource _cancellationTokenSource = new();

    private readonly TaskCompletionSource<UpdateInfo?> _updateTask = new();

    private Task? _backgroundTask;

    public Task OnStartedAsync()
    {
        this._backgroundTask = Task.Run(this.RunNewVersionCheck, this._cancellationTokenSource.Token);

        return this._backgroundTask.IsCompleted ? this._backgroundTask : Task.CompletedTask;
    }

    public async Task<UpdateInfo?> GetLatestVersionAsync(CancellationToken token = default)
    {
        return await this._updateTask.Task;
    }

    protected override async ValueTask DisposeAsync(bool disposing)
    {
        if (disposing)
        {
            if (this._backgroundTask != null)
            {
                await this._cancellationTokenSource.CancelAsync();

                try
                {
                    await this._backgroundTask;
                }
                catch (Exception)
                {
                    // nothing
                }
            }

            this._updateTask.SetCanceled();

            try
            {
                await this._updateTask.Task;
            }
            catch (Exception)
            {
                // nothing
            }
        }
    }

    private async Task RunNewVersionCheck()
    {
        var mgr = new UpdateManager("https://github.com/ChangemakerStudios/Papercut-SMTP", logger: logger);

        if (mgr.IsInstalled)
        {
            try
            {
                // check for new version
                var newVersion = await mgr.CheckForUpdatesAsync();
                if (newVersion != null)
                {
                    logger.LogInformation("New Version of Papercut SMTP is Available {@NewVersion}", newVersion);

                    this._updateTask.SetResult(newVersion);
                }
            }
            catch (Exception ex)
            {
                this._updateTask.SetException(ex);
            }
        }
        else
        {
            logger.LogDebug("Papercut was not installed via Velopack. Cannot check for new versions.");
        }

        this._updateTask.SetResult(null);

        // for testing
        //this._updateTask.SetResult(new UpdateInfo(new VelopackAsset() { Version = new SemanticVersion(10, 0, 0) }, false));
    }

    #region Begin Static Container Registrations

    static void Register(ContainerBuilder builder)
    {
        builder.RegisterType<NewVersionCheckHandler>().As<IAppLifecycleStarted>().As<INewVersionProvider>().InstancePerUIScope();
    }

    #endregion
}