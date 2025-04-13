// Papercut
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


using System.Reactive.Concurrency;
using System.Reactive.Threading.Tasks;

using MimeKit;

using Papercut.Core.Domain.Message;

namespace Papercut.Message.Helpers;

public static class MimeMessageLoaderExtensions
{
    public static async Task<MimeMessage> GetClonedAsync(this MimeMessageLoader loader, MessageEntry entry, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(loader);
        ArgumentNullException.ThrowIfNull(entry);

        var message = await loader.GetAsync(entry, token);
        return await message.CloneMessageAsync(token);
    }

    public static IObservable<MimeMessage?> GetObservable(this MimeMessageLoader loader, MessageEntry entry, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(loader);
        ArgumentNullException.ThrowIfNull(entry);

        return loader.GetAsync(entry, token).ToObservable(TaskPoolScheduler.Default);
    }
}