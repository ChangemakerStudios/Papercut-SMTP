/*  
 * Papercut
 *
 *  Copyright © 2008 - 2012 Ken Robertson
 *  Copyright © 2013 - 2014 Jaben Cargman
 *  
 *  Licensed under the Apache License, Version 2.0 (the "License");
 *  you may not use this file except in compliance with the License.
 *  You may obtain a copy of the License at
 *  
 *  http://www.apache.org/licenses/LICENSE-2.0
 *  
 *  Unless required by applicable law or agreed to in writing, software
 *  distributed under the License is distributed on an "AS IS" BASIS,
 *  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 *  See the License for the specific language governing permissions and
 *  limitations under the License.
 *  
 */

namespace Papercut.Events
{
    using Caliburn.Micro;

    using Papercut.Core.Events;

    public class EventPublishAll : IPublishEvent
    {
        readonly AutofacPublishEvent _autofacPublishEvent;

        readonly IEventAggregator _uiEventAggregator;

        public EventPublishAll(
            AutofacPublishEvent autofacPublishEvent,
            IEventAggregator uiEventAggregator)
        {
            _autofacPublishEvent = autofacPublishEvent;
            _uiEventAggregator = uiEventAggregator;
        }

        public void Publish<T>(T eventObject) where T : IDomainEvent
        {
            _autofacPublishEvent.Publish(eventObject);
            _uiEventAggregator.Publish(eventObject);
        }
    }
}