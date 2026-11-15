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


namespace Papercut.Service.Application.Messages;

using Core.Domain.Message;

using Domain.Messages;

using Papercut.Message.Index;

/// <summary>
/// The message list behind the web UI and MCP list_messages: search, paging and read
/// status. Rows come from the message index; any it doesn't have yet (or all of them,
/// when it couldn't open) are read from the files as before.
/// </summary>
public class MessageListService(
    IMessageRepository messageRepository,
    IMimeMessageLoader messageLoader,
    IMessageIndex index,
    MessageReadStateService readState)
{
    public List<MessageEntry> LoadEntries() => messageRepository.LoadMessages().ToList();

    public async Task<GetMessagesResponse> ListAsync(
        IReadOnlyList<MessageEntry> entries,
        int start,
        int limit,
        bool ascending,
        string? search,
        CancellationToken token = default)
    {
        var matching = await FilterAsync(entries, search, token);

        var ordered = ascending
            ? matching.OrderBy(msg => msg.ModifiedDate)
            : matching.OrderByDescending(msg => msg.ModifiedDate);

        var page = ordered.Skip(Math.Max(0, start)).Take(Math.Max(0, limit)).ToList();

        var summaries = await index.GetSummariesAsync(page, token);

        var messages = await Task.WhenAll(page.Select(async entry =>
        {
            var dto = summaries.TryGetValue(entry.File, out var summary)
                ? RefDto.CreateFrom(entry, summary)
                : RefDto.CreateFrom(new MimeMessageEntry(entry, (await messageLoader.GetAsync(entry, token))!));

            dto.IsRead = IsRead(entry);

            return dto;
        })).WaitAsync(token);

        return new GetMessagesResponse(matching.Count, messages, entries.Count);
    }

    /// <summary>
    /// Opened, here or in an earlier session. Without the index, only this session's opens
    /// are known, so anything older than a few minutes counts as seen.
    /// </summary>
    public bool IsRead(MessageEntry entry) =>
        readState.IsRead(entry.Id) || (index.IsAvailable ? index.IsRead(entry.File) : entry.HasBeenSeen);

    public void MarkRead(MessageEntry entry)
    {
        readState.MarkRead(entry.Id);
        index.MarkRead(entry.File);
    }

    /// <summary>Read again once opened. Without the index, mail older than a few minutes still counts as seen.</summary>
    public void MarkUnread(MessageEntry entry)
    {
        readState.MarkUnread(entry.Id);
        index.MarkUnread(entry.File);

        // the list's ETag only moves with the version, which removing may not have bumped
        readState.Touch();
    }

    public void MarkAllRead()
    {
        readState.MarkRead(LoadEntries().Select(e => e.Id));
        index.MarkAllRead();
        readState.Touch();
    }

    /// <summary>
    /// Full text once the index has seen every message; until then (first start on a big
    /// inbox), sender, recipient and subject read from the files.
    /// </summary>
    async Task<List<MessageEntry>> FilterAsync(IReadOnlyList<MessageEntry> entries, string? search, CancellationToken token)
    {
        if (MessageSearch.Terms(search).Length == 0) return entries.ToList();

        if (!index.IsBuilt) return await MessageSearch.FilterAsync(entries, messageLoader, search, token);

        // picks up mail that arrived or was edited since the last look
        await index.SyncAsync(entries, token);

        var files = await index.SearchAsync(search!, token);

        return entries.Where(e => files.Contains(e.File)).ToList();
    }
}
