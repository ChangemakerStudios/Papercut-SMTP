// Papercut
//
// Copyright © 2008 - 2012 Ken Robertson
// Copyright © 2013 - 2025 Jaben Cargman
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

using System.Collections.Concurrent;

namespace Papercut.Service.Domain.Messages;

/// <summary>
///     Which messages have been opened, for the lifetime of the service.
///     <para>
///     <see cref="Core.Domain.Message.MessageEntry.HasBeenSeen" /> is derived from the
///     message's age -- anything older than five minutes reports as seen. That works for
///     the desktop app, which holds one long-lived list in memory and flips the flag when
///     you select a row. The web api builds its entries per request, so without this every
///     message would go un-bold five minutes after it arrived whether or not anyone read
///     it, and a message you had just opened would stay bold until the clock caught up.
///     </para>
///     <para>
///     In memory only, exactly like the desktop's flag: a restart forgets, and the age
///     heuristic still covers everything that arrived before the service started.
///     </para>
/// </summary>
public class MessageReadStateService
{
    private readonly ConcurrentDictionary<string, byte> _read = new(StringComparer.Ordinal);

    /// <summary>
    ///     Bumped on every change so <c>GetAll</c> can fold it into its ETag -- otherwise
    ///     opening a message would 304 the list and leave it showing the stale bold row.
    /// </summary>
    public long Version { get; private set; }

    public bool IsRead(string messageId) => _read.ContainsKey(messageId);

    public void MarkRead(string messageId)
    {
        if (string.IsNullOrEmpty(messageId)) return;

        if (_read.TryAdd(messageId, 0))
        {
            Version++;
        }
    }
}
