// Papercut
// 
// Copyright © 2008 - 2012 Ken Robertson
// Copyright © 2013 - 2020 Jaben Cargman
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

namespace Papercut.Events
{
    using System;
    using System.Threading.Tasks;

    using Autofac;

    using Caliburn.Micro;

    using Papercut.Common.Domain;
    using Papercut.Core.Annotations;
    using Papercut.Core.Infrastructure.MessageBus;

    public class EventPublishAll : AutofacMessageBus
    {
        readonly IEventAggregator _uiEventAggregator;

        public EventPublishAll(
            ILifetimeScope scope,
            IEventAggregator uiEventAggregator)
            : base(scope)
        {
            _uiEventAggregator = uiEventAggregator;
        }

        public override void Publish<T>([NotNull] T eventObject)
        {
            if (eventObject == null) throw new ArgumentNullException(nameof(eventObject));

            base.Publish(eventObject);

            _uiEventAggregator.PublishOnUIThread(eventObject);
        }

        protected override void ExecuteHandler<T>(T eventObject, IEventHandler<T> @event)
        {
            if (@event is IUIThreadEventHandler<T>)
            {
                PlatformProvider.Current.OnUIThread(
                    () =>
                {
                    base.ExecuteHandler(eventObject, @event);
                });
            }
            else
            {
                base.ExecuteHandler(eventObject, @event);
            }
        }
    }
}