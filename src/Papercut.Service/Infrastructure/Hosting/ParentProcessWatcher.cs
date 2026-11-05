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

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Papercut.Service.Infrastructure.Hosting;

/// <summary>
/// When "ParentProcessId" is set (the desktop shell runs the service as its child), stop when
/// the parent exits or closes our stdin. Closing stdin is the shell's graceful stop on every OS.
/// </summary>
public class ParentProcessWatcher(IConfiguration configuration, IHostApplicationLifetime lifetime, ILogger logger)
    : BackgroundService
{
    public const string ConfigKey = "ParentProcessId";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!int.TryParse(configuration[ConfigKey], out var parentId)) return;

        var stopReason = await Task.WhenAny(WaitForParentExitAsync(parentId, stoppingToken), WaitForStdinCloseAsync(stoppingToken));

        if (stoppingToken.IsCancellationRequested) return;

        logger.Information("Stopping: {Reason}", await stopReason);

        lifetime.StopApplication();
    }

    private async Task<string> WaitForParentExitAsync(int parentId, CancellationToken token)
    {
        try
        {
            using var parent = Process.GetProcessById(parentId);

            logger.Information("Running as a child of process {ParentProcessId}", parentId);

            await parent.WaitForExitAsync(token);
        }
        catch (ArgumentException)
        {
            // already gone
        }
        catch (OperationCanceledException)
        {
        }

        return $"parent process {parentId} exited";
    }

    private static async Task<string> WaitForStdinCloseAsync(CancellationToken token)
    {
        if (!Console.IsInputRedirected)
        {
            await Task.Delay(Timeout.Infinite, token).ContinueWith(_ => { }, TaskScheduler.Default);
            return "shutting down";
        }

        // ReadAsync on a redirected stdin pipe returns 0 at end of stream
        await using var stdin = Console.OpenStandardInput();
        var buffer = new byte[256];

        try
        {
            while (await stdin.ReadAsync(buffer, token) > 0)
            {
            }
        }
        catch (OperationCanceledException)
        {
        }

        return "stdin closed";
    }
}
