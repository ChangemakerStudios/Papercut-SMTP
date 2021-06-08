// Papercut
// 
// Copyright © 2008 - 2012 Ken Robertson
// Copyright © 2013 - 2021 Jaben Cargman
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


namespace Papercut.Core.Infrastructure.MessageBus
{
    using System;
    using System.Threading.Tasks;

    using Papercut.Common.Domain;
    using Papercut.Core.Annotations;

    using Serilog;

    public static class MessageBusExtensions
    {
        public static void PublishFireAndForget<T>([NotNull] this IMessageBus messageBus, T @event)
            where T : IEvent
        {
            if (messageBus == null) throw new ArgumentNullException(nameof(messageBus));

            Task.Run(() =>
            {
                try
                {
                    messageBus.PublishAsync(@event, default);
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "Exception Publishing Event {EventType}", typeof(T).FullName);
                }
            });
        }
    }
}