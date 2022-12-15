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


namespace Papercut.AppLayer.Events
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    using Autofac;

    using Caliburn.Micro;

    using Papercut.Common.Domain;
    using Papercut.Core.Annotations;
    using Papercut.Core.Infrastructure.MessageBus;

    public class EventPublishAll : AutofacMessageBus
    {
        private readonly IEventAggregator _eventAggregator;

        public EventPublishAll(
            ILifetimeScope scope,
            IEventAggregator eventAggregator)
            : base(scope)
        {
            this._eventAggregator = eventAggregator;
        }

        public override async Task PublishAsync<T>([NotNull] T eventObject, CancellationToken token)
        {
            if (eventObject == null) throw new ArgumentNullException(nameof(eventObject));

            await base.PublishAsync(eventObject, token);
            await this._eventAggregator.PublishOnUIThreadAsync(eventObject, token);
        }

        protected override Task HandleAsync<T>(T eventObject, IEventHandler<T> @event, CancellationToken token)
        {
            var taskCompletionSource = new TaskCompletionSource<bool>();

            async void InnerHandle()
            {
                try
                {
                    await base.HandleAsync(eventObject, @event, token);

                    taskCompletionSource.SetResult(true);
                }
                catch (OperationCanceledException)
                {
                    taskCompletionSource.SetCanceled();
                }
                catch (Exception ex)
                {
                    taskCompletionSource.SetException(ex);
                }
            }

            Execute.BeginOnUIThread(InnerHandle);

            return taskCompletionSource.Task;
        }

        #region Begin Static Container Registrations

        /// <summary>
        /// Called dynamically from the RegisterStaticMethods() call in the container module.
        /// </summary>
        /// <param name="builder"></param>
        [UsedImplicitly]
        static void Register([NotNull] ContainerBuilder builder)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));

            builder.RegisterType<EventPublishAll>().As<IMessageBus>().InstancePerLifetimeScope();
        }

        #endregion
    }
}