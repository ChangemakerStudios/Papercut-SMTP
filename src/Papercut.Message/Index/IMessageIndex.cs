// Papercut
//
// Copyright © 2008 - 2012 Ken Robertson
// Copyright © 2013 - 2026 Jaben Cargman
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


using Papercut.Core.Domain.Message;

namespace Papercut.Message.Index;

public sealed record IndexedAddress(string? Name, string Address);

/// <summary>A list row, without opening the file.</summary>
public sealed record MessageSummary(
    string? Subject,
    IReadOnlyList<IndexedAddress> From,
    string Priority,
    int AttachmentCount,
    bool IsRead);

public interface IMessageIndex : IDisposable
{
    /// <summary>False when the database could not be opened; callers read the files instead.</summary>
    bool IsAvailable { get; }

    /// <summary>The first full pass is done, so search covers every message.</summary>
    bool IsBuilt { get; }

    /// <summary>Indexes new and changed files and drops rows for files that are gone.</summary>
    Task SyncAsync(IReadOnlyCollection<MessageEntry> entries, CancellationToken token = default);

    /// <summary>
    /// Summaries for these entries, indexing any that are missing unless a build is running.
    /// Entries left out have to be read from their files.
    /// </summary>
    Task<IReadOnlyDictionary<string, MessageSummary>> GetSummariesAsync(IReadOnlyCollection<MessageEntry> entries, CancellationToken token = default);

    /// <summary>Files where every word appears in the subject, an address, the body or an attachment name.</summary>
    Task<IReadOnlySet<string>> SearchAsync(string search, CancellationToken token = default);

    bool IsRead(string file);

    void MarkRead(string file);

    void MarkAllRead();
}
