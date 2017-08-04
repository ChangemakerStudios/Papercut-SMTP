// Papercut
// 
// Copyright © 2008 - 2012 Ken Robertson
// Copyright © 2013 - 2017 Jaben Cargman
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

namespace Papercut.Message
{
    using System;
    using System.IO;
    using System.Reactive;
    using System.Reactive.Concurrency;
    using System.Reactive.Disposables;
    using System.Reactive.Linq;
    using System.Runtime.Caching;
    using System.Threading;
    using System.Threading.Tasks;

    using MimeKit;

    using Papercut.Common.Extensions;
    using Papercut.Core.Domain.Message;

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
                    var observer = Observer.Synchronize(o, true);
                    var disposable = new CancellationDisposable();

                    MimeMessage message = null;

                    try
                    {
                        message = this.GetMimeMessageFromCache(messageEntry);
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

                    if (message != null)
                    {
                        observer.OnNext(message);
                        observer.OnCompleted();
                    }

                    return disposable;
                }).SubscribeOn(TaskPoolScheduler.Default);
        }

        private MimeMessage GetMimeMessageFromCache(MessageEntry messageEntry)
        {
            return MimeMessageCache.GetOrSet(
                messageEntry.File,
                () =>
                {
                    this._logger.Verbose(
                        "Getting Message Data from Message Repository",
                        messageEntry);

                    var messageData = this._messageRepository.GetMessage(messageEntry);
                    MimeMessage mimeMessage;

                    // wrap in a memorystream...
                    using (var ms = new MemoryStream(messageData))
                    {
                        this._logger.Verbose(
                            "MimeMessage Load for {@MessageEntry}",
                            messageEntry);

                        mimeMessage = MimeMessage.Load(ParserOptions.Default, ms);
                    }

                    return mimeMessage;
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
    }
}