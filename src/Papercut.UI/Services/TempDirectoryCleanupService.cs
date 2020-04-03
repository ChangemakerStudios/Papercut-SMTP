// Papercut
// 
// Copyright © 2008 - 2012 Ken Robertson
// Copyright © 2013 - 2020 Jaben Cargman
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

namespace Papercut.Services
{
    using System;
    using System.IO;
    using System.Threading.Tasks;

    using Papercut.Common.Domain;
    using Papercut.Core.Domain.Application;
    using Papercut.Core.Infrastructure.Lifecycle;

    using Serilog;

    public class TempDirectoryCleanupService : IEventHandler<PapercutClientExitEvent>
    {
        readonly IAppMeta _appMeta;

        readonly ILogger _logger;

        public TempDirectoryCleanupService(IAppMeta appMeta, ILogger logger)
        {
            _appMeta = appMeta;
            _logger = logger;
        }

        public void Handle(PapercutClientExitEvent @event)
        {
            // time for temp file cleanup
            TryCleanUpTempDirectories();
        }

        private void TryCleanUpTempDirectories()
        {
            int deleteCount = 0;
            string tempPath = Path.GetTempPath();

            // try cleanup...
            try
            {
                string[] tmpDirs = Directory.GetDirectories(tempPath, $"{this._appMeta.AppName}-*");

                foreach (string tmpDir in tmpDirs)
                {
                    try
                    {
                        Directory.Delete(tmpDir, true);
                        deleteCount++;
                    }
                    catch (Exception ex)
                    {
                        _logger.Warning(ex, @"Unable to delete {TempDirectory}", tmpDir);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Warning(
                    ex,
                    @"Failure running temp directory cleanup on temp path {TempPath}",
                    tempPath);
            }

            if (deleteCount > 0)
                _logger.Information("Deleted {DeleteCount} temporary directories", deleteCount);
        }
    }
}