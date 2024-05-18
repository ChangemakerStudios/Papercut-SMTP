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


using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using Velopack;

namespace Papercut.Service.Infrastructure.NewVersionCheck;

public class NewVersionCheckHost(ILogger<NewVersionCheckHost> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var mgr = new UpdateManager("https://github.com/ChangemakerStudios/Papercut-SMTP", logger: logger);

        if (mgr.IsInstalled)
        {
            // check for new version
            var newVersion = await mgr.CheckForUpdatesAsync();
            if (newVersion != null)
            {
                logger.LogInformation("New Version of Papercut SMTP Service is Available: {Version}", newVersion.TargetFullRelease.Version);
            }
        }
    }
}