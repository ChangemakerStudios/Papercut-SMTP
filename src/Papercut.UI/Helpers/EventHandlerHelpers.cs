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


namespace Papercut.Helpers
{
    using System;
    using System.Reactive;
    using System.Reactive.Linq;

    using Papercut.Core.Annotations;

    public static class EventHandlerHelpers
    {
        public static IObservable<EventPattern<object>> ToObservable([NotNull] this EventHandler eventHandler)
        {
            if (eventHandler == null) throw new ArgumentNullException(nameof(eventHandler));

            return Observable.FromEventPattern(
                a => eventHandler += a,
                d => eventHandler += d);
        }

        public static IObservable<EventPattern<TArgs>> ToObservable<TArgs>([NotNull] this EventHandler<TArgs> eventHandler)
            where TArgs : EventArgs
        {
            if (eventHandler == null) throw new ArgumentNullException(nameof(eventHandler));

            return Observable.FromEventPattern<EventHandler<TArgs>, TArgs>(
                a => eventHandler += a,
                d => eventHandler += d);
        }
    }
}