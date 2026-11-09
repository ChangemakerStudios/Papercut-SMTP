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


namespace Papercut.Service.Infrastructure.MessageWatching;

using System.Reactive.Linq;

using Microsoft.Extensions.Hosting;

public class MessageWatcherHostedService(Lazy<MessageWatcher> messageWatcher, IMessageBus messageBus, ILogger logger) : IHostedService
{
    private MessageWatcher _watcher;
    private IDisposable? _refreshSubscription;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        logger.Information("Starting message watcher...");

        _watcher = messageWatcher.Value;

        _watcher.NewMessage += async (sender, args) =>
        {
            await messageBus.PublishAsync(new NewMessageEvent(args.NewMessage.ToDto()), cancellationToken);
        };

        // a cleanup rule deletes in a burst: one refresh for the lot, not one per file
        _refreshSubscription = Observable.FromEventPattern(
                e => _watcher.RefreshNeeded += e,
                e => _watcher.RefreshNeeded -= e)
            .Throttle(TimeSpan.FromMilliseconds(100))
            .Subscribe(async _ =>
            {
                try
                {
                    await messageBus.PublishAsync(new MessageListChangedEvent(), CancellationToken.None);
                }
                catch (Exception ex)
                {
                    logger.Error(ex, "Error publishing message list change");
                }
            });

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _refreshSubscription?.Dispose();
        _watcher.Dispose();
        
        return Task.CompletedTask;
    }
}