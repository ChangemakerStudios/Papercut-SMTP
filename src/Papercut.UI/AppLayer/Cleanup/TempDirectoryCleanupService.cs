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


using System.IO;

using Autofac;

using Papercut.Core.Domain.Application;
using Papercut.Domain.LifecycleHooks;

namespace Papercut.AppLayer.Cleanup;

public class TempDirectoryCleanupService(IAppMeta appMeta, ILogger logger) : IAppLifecyclePreExit
{
    public Task<AppLifecycleActionResultType> OnPreExit()
    {
        // time for temp file cleanup
        this.TryCleanUpTempDirectories();

        return Task.FromResult(AppLifecycleActionResultType.Continue);
    }

    private void TryCleanUpTempDirectories()
    {
        int deleteCount = 0;
        string tempPath = Path.GetTempPath();

        // try cleanup...
        try
        {
            string[] tmpDirs = Directory.GetDirectories(tempPath, $"{appMeta.AppName}-*");

            foreach (string tmpDir in tmpDirs)
            {
                try
                {
                    Directory.Delete(tmpDir, true);
                    deleteCount++;
                }
                catch (Exception ex)
                {
                    logger.Warning(ex, @"Unable to delete {TempDirectory}", tmpDir);
                }
            }
        }
        catch (Exception ex)
        {
            logger.Warning(
                ex,
                @"Failure running temp directory cleanup on temp path {TempPath}",
                tempPath);
        }

        if (deleteCount > 0)
            logger.Information("Deleted {DeleteCount} temporary directories", deleteCount);
    }

    #region Begin Static Container Registrations

    /// <summary>
    /// Called dynamically from the RegisterStaticMethods() call in the container module.
    /// </summary>
    /// <param name="builder"></param>
    [UsedImplicitly]
    static void Register(ContainerBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RegisterType<TempDirectoryCleanupService>().AsImplementedInterfaces()
            .SingleInstance();
    }

    #endregion
}