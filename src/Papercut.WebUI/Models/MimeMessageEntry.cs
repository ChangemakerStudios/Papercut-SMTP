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


namespace Papercut.WebUI.Models
{
    using System;
    using System.Threading.Tasks;

    using Core.Domain.Message;

    using Message;

    public class MimeMessageEntry : MessageEntry
    {
        public string Subject { get; protected set; }

        public MimeMessageEntry(MessageEntry entry, MimeMessageLoader loader) : base(entry.File)
        {
            loader.Get(this).ToTask().ContinueWith(e =>
            {
                Subject = e.IsFaulted ? "Failure loading message: " + e.Exception?.Message : e.Result.Subject;
            }).Wait();
        }

        public class Dto
        {
            public static Dto From(MimeMessageEntry messageEntry)
            {
                return new Dto
                {
                    Subject = messageEntry.Subject,
                    CreatedAt = messageEntry._created,
                    Id = messageEntry.Name,
                    Size = messageEntry.FileSize
                };
            }

            public string Size { get; set; }

            public string Id { get; set; }

            public DateTime? CreatedAt { get; set; }

            public string Subject { get; set; }
        }
    }

    static class ObservableExtensions
    {
        public static Task<T> ToTask<T>(this IObservable<T> observable)
        {
            var taskCompleteSource = new TaskCompletionSource<T>();

            observable.Subscribe(
                m => { taskCompleteSource.SetResult(m); },
                e => { taskCompleteSource.SetException(e); });

            return taskCompleteSource.Task;
        }
    }
}