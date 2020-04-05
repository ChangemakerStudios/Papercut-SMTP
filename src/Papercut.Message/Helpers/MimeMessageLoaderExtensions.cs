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


namespace Papercut.Message.Helpers
{
    using System;
    using System.Reactive.Concurrency;
    using System.Reactive.Threading.Tasks;
    using System.Threading.Tasks;

    using Core.Annotations;
    using Core.Domain.Message;

    using MimeKit;

    public static class MimeMessageLoaderExtensions
    {
        public static MimeMessage Get([NotNull] this MimeMessageLoader loader, [NotNull] MessageEntry entry)
        {
            if (loader == null) throw new ArgumentNullException(nameof(loader));
            if (entry == null) throw new ArgumentNullException(nameof(entry));

            var loadTask = loader.GetAsync(entry);
            loadTask.Wait();

            return loadTask.Result;
        }

        public static IObservable<MimeMessage> GetObservable([NotNull] this MimeMessageLoader loader, [NotNull] MessageEntry entry)
        {
            if (loader == null) throw new ArgumentNullException(nameof(loader));
            if (entry == null) throw new ArgumentNullException(nameof(entry));

            return loader.GetAsync(entry).ToObservable(TaskPoolScheduler.Default);
        }
    }
}