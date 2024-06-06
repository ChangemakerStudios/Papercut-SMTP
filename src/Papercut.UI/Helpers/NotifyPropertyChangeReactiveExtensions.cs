﻿// Papercut
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


using System.ComponentModel;
using System.Linq.Expressions;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Windows.Threading;

namespace Papercut.Helpers
{
    public static class NotifyPropertyChangeReactiveExtensions
    {
        // Returns the values of property (an Expression) as they
        // change, starting with the current value
        public static IObservable<TValue> GetPropertyValues<TSource, TValue>(
            this TSource source,
            Expression<Func<TSource, TValue>> property,
            IScheduler? scheduler = null)
            where TSource : INotifyPropertyChanged
        {
            if (property.Body is not MemberExpression memberExpression)
            {
                throw new ArgumentException(
                    "property must directly access a property of the source");
            }

            string propertyName = memberExpression.Member.Name;

            Func<TSource, TValue> accessor = property.Compile();

            return source.GetPropertyChangedEvents(scheduler)
                .Where(x => x.EventArgs.PropertyName == propertyName)
                .Select(_ => accessor(source))
                .StartWith(accessor(source));
        }

        // This is a wrapper around FromEvent(PropertyChanged)
        public static IObservable<EventPattern<PropertyChangedEventArgs>> GetPropertyChangedEvents(
            this INotifyPropertyChanged source,
            IScheduler? scheduler = null)
        {
            return
                Observable.FromEventPattern<PropertyChangedEventHandler, PropertyChangedEventArgs>(
                    h => new PropertyChangedEventHandler(h),
                    h => source.PropertyChanged += h,
                    h => source.PropertyChanged -= h,
                    scheduler ?? Scheduler.Default);
        }

        public static IDisposable
            Subscribe<TSource, TValue>(
            this TSource source,
            Expression<Func<TSource, TValue>> property,
            Action<TValue> observer,
            IScheduler? scheduler = null)
            where TSource : INotifyPropertyChanged
        {
            return source
                .GetPropertyValues(property, scheduler)
                .ObserveOn(Dispatcher.CurrentDispatcher)
                .Subscribe(observer);
        }
    }
}