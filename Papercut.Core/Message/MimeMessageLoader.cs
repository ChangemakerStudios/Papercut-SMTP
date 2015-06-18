// Papercut
// 
// Copyright © 2008 - 2012 Ken Robertson
// Copyright © 2013 - 2014 Jaben Cargman
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

namespace Papercut.Core.Message
{
    using System;
    using System.IO;
    using System.Reactive;
    using System.Reactive.Concurrency;
    using System.Reactive.Disposables;
    using System.Reactive.Linq;
    using System.Runtime.Caching;

    using MimeKit;

    using Papercut.Core.Helper;

    using Serilog;

    public class MimeMessageLoader
    {
        public static MemoryCache MimeMessageCache;

        readonly ILogger _logger;

        readonly MessageRepository _messageRepository;

        static MimeMessageLoader()
        {
            MimeMessageCache = new MemoryCache("MimeMessage");
        }

        public MimeMessageLoader(MessageRepository messageRepository, ILogger logger)
        {
            _messageRepository = messageRepository;
            _logger = logger.ForContext<MimeMessageLoader>();
        }

        public IObservable<MimeMessage> Get(MessageEntry messageEntry)
        {
            _logger.Verbose("Loading Message Entry {@MessageEntry}", messageEntry);

            return Observable.Create<MimeMessage>(
                o =>
                {
                    // in case of multiple subscriptions...
                    var observer = Observer.Synchronize(o);

                    var disposable = new CancellationDisposable();

                    try
                    {
                        var message = MimeMessageCache.GetOrSet(
                            messageEntry.File,
                            () =>
                            {
                                _logger.Verbose(
                                    "Getting Message Data from Cached Message Repository",
                                    messageEntry);
                                var messageData = _messageRepository.GetMessage(messageEntry);
                                MimeMessage mimeMessage;

                                // wrap in a memorystream...
                                using (var ms = new MemoryStream(messageData))
                                {
                                    _logger.Verbose(
                                        "MimeMessage Load for {@MessageEntry}",
                                        messageEntry);

                                    mimeMessage = MimeMessage.Load(
                                        ParserOptions.Default,
                                        ms,
                                        disposable.Token);
                                }

                                return mimeMessage;
                            },
                            m =>
                            {
                                var policy = new CacheItemPolicy
                                {
                                    SlidingExpiration = TimeSpan.FromSeconds(300)
                                };

                                MimeMessageCache.Add(messageEntry.File, m, policy);
                            });

                        observer.OnNext(message);
                        observer.OnCompleted();
                    }
                    catch (OperationCanceledException)
                    {
                        // no need to respond...
                    }
                    catch (Exception ex)
                    {
                        _logger.Error(ex, "Exception Loading {@MessageEntry}", messageEntry);
                        observer.OnError(ex);
                    }

                    return disposable;
                }).SubscribeOn(TaskPoolScheduler.Default);
        }
    }
}