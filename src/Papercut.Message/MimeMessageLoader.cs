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


using System.Collections.Concurrent;
using System.Runtime.Caching;

using Autofac.Util;

using MimeKit;

using Papercut.Common.Extensions;
using Papercut.Core.Domain.Message;

using Serilog.Events;

namespace Papercut.Message
{
    public class MimeMessageLoader : Disposable
    {
        public static MemoryCache MimeMessageCache;

        private readonly Task[] _backgroundLoaders;

        private readonly CancellationTokenSource _cancellationSource;

        readonly ILogger _logger;

        readonly MessageRepository _messageRepository;

        private readonly ConcurrentQueue<MessageLoadRequest?> _queue =
            new ConcurrentQueue<MessageLoadRequest?>();

        static MimeMessageLoader()
        {
            MimeMessageCache = new MemoryCache(nameof(MimeMessage));
        }

        public MimeMessageLoader(MessageRepository messageRepository, ILogger logger)
        {
            this._messageRepository = messageRepository;
            this._logger = logger.ForContext<MimeMessageLoader>();
            this._cancellationSource = new CancellationTokenSource();

            // run two background loaders
            this._backgroundLoaders = new[]
                                      {
                                          Task.Run(
                                              this.LoopAsync,
                                              this._cancellationSource.Token)
                                      };
        }

        private async Task LoopAsync()
        {
            try
            {
                while (!this._cancellationSource.IsCancellationRequested)
                {
                    if (this._queue.IsEmpty)
                    {
                        await Task.Delay(50, this._cancellationSource.Token);
                        continue;
                    }

                    var tasks = new List<Task>();

                    for (int i = 0; i < 4; i++)
                    {
                        if (this._cancellationSource.IsCancellationRequested)
                        {
                            return;
                        }

                        if (this._queue.TryDequeue(out MessageLoadRequest? request) && request is not null)
                        {
                            tasks.Add(
                                this.GetMimeMessageFromCacheAsync(
                                        request.MessageEntry,
                                        this._cancellationSource.Token)
                                    .ContinueWith(
                                        t =>
                                        {
                                            request.InvokeCallback(t.Result);
                                        }));
                        }
                        else
                        {
                            break;
                        }
                    }

                    await Task.WhenAll(tasks);
                }
            }
            catch (Exception e) when (e is ObjectDisposedException or TaskCanceledException)
            {
                // no need
            }
        }

        protected override async ValueTask DisposeAsync(bool disposing)
        {
            if (disposing)
            {
                await this._cancellationSource.CancelAsync();

                try
                {
                    await Task.WhenAll(this._backgroundLoaders);
                }
                catch (Exception)
                {
                    // catch all
                }
            }
        }

        public void GetMessageCallback(
            MessageEntry messageEntry,
            Action<MimeMessage> callback)
        {
            this._queue.Enqueue(new MessageLoadRequest(messageEntry, callback));
        }

        public Task<MimeMessage> GetAsync(MessageEntry messageEntry, CancellationToken token = default)
        {
            return this.GetMimeMessageFromCacheAsync(messageEntry, token);
        }

        private async Task<MimeMessage> GetMimeMessageFromCacheAsync(MessageEntry messageEntry, CancellationToken token = default)
        {
            return await MimeMessageCache.GetOrSetAsync(
                messageEntry.File,
                async () =>
                {
                    if (this._logger.IsEnabled(LogEventLevel.Verbose))
                    {
                        this._logger.Verbose(
                            "Loading Message for File {MessageFile}",
                            messageEntry.File);
                    }

                    var message = await this._messageRepository.GetMessage(messageEntry);

                    using var ms = new MemoryStream(message);

                    return await MimeMessage.LoadAsync(ParserOptions.Default, ms, token);
                },
                m =>
                {
                    var policy = new CacheItemPolicy
                                 {
                                     SlidingExpiration = TimeSpan.FromSeconds(10)
                                 };

                    MimeMessageCache.Add(messageEntry.File, m, policy);
                });
        }

        protected class MessageLoadRequest(MessageEntry messageEntry, Action<MimeMessage> callback)
        {
            public MessageEntry MessageEntry { get; } = messageEntry;

            private Action<MimeMessage> Callback { get; } = callback;

            public void InvokeCallback(MimeMessage mimeMessage)
            {
                this.Callback.Invoke(mimeMessage);
            }
        }
    }
}