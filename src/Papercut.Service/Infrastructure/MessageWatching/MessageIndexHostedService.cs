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


namespace Papercut.Service.Infrastructure.MessageWatching;

using System.Diagnostics;

using Microsoft.Extensions.Hosting;

using Papercut.Message.Index;

/// <summary>
/// Brings the message index up to date at startup, in the background: the first run on a
/// big inbox takes a while, and the list and header search work from the files meanwhile.
/// </summary>
public class MessageIndexHostedService(IMessageIndex index, IMessageRepository messageRepository, ILogger logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!index.IsAvailable) return;

        // let the server finish starting first
        await Task.Yield();

        try
        {
            var stopwatch = Stopwatch.StartNew();
            var entries = messageRepository.LoadMessages().ToList();

            await index.SyncAsync(entries, stoppingToken);

            logger.Information("Message index ready: {MessageCount} messages in {Elapsed:0.0}s", entries.Count, stopwatch.Elapsed.TotalSeconds);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // shutting down mid-build; the next start carries on from what was saved
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Failed to build the message index");
        }
    }
}
