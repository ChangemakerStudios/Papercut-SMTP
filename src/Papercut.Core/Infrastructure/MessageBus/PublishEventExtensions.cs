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
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;

    using Papercut.Common.Domain;
    using Papercut.Core.Annotations;

    [PublicAPI]
    public static class PublishEventExtensions
    {
        static readonly MethodInfo _publishAsyncMethodInfo = typeof(IMessageBus).GetMethod(nameof(IMessageBus.PublishAsync));

        public static Task PublishObjectAsync(
            this IMessageBus messageBus,
            object @event,
            Type eventType,
            CancellationToken token = default)
        {
            if (messageBus == null) throw new ArgumentNullException(nameof(messageBus));

            MethodInfo publishMethod = _publishAsyncMethodInfo.MakeGenericMethod(eventType);

            return (Task)publishMethod.Invoke(messageBus, new[] { @event, token });
        }
    }
}