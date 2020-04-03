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

namespace Papercut.Rules.Helpers
{
    using System;
    using System.Reactive.Linq;

    public static class ObservableExtensions
    {
        public static IObservable<T> RetryWithDelay<T>(this IObservable<T> source, int retryCount, TimeSpan timeSpan)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (timeSpan < TimeSpan.Zero)
                throw new ArgumentOutOfRangeException("timeSpan");
            if (timeSpan == TimeSpan.Zero)
                return source.Retry(retryCount);

            return source.Catch(Observable.Timer(timeSpan).SelectMany(_ => source).Retry(retryCount));
        }
    }
}