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


using MimeKit;

using Papercut.Core.Domain.Message;

namespace Papercut.Message;

/// <summary>
/// Inbox search by sender, recipient and subject. Shared by the REST list and MCP list_messages.
/// </summary>
public static class MessageSearch
{
    /// <summary>
    /// Every word must appear somewhere in the subject, From, To or Cc (names or addresses),
    /// ignoring case. Blank matches everything.
    /// </summary>
    public static bool Matches(MimeMessage? message, string? search)
    {
        var terms = Terms(search);
        if (terms.Length == 0) return true;
        if (message == null) return false;

        var haystack = string.Join('\n',
            message.Subject ?? "",
            Describe(message.From),
            Describe(message.To),
            Describe(message.Cc));

        return terms.All(term => haystack.Contains(term, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// The entries whose message matches, in the order given. Headers only, so the
    /// loader's shared cached instances are safe to read side by side.
    /// </summary>
    public static async Task<List<MessageEntry>> FilterAsync(
        IEnumerable<MessageEntry> entries,
        IMimeMessageLoader loader,
        string? search,
        CancellationToken token = default)
    {
        var list = entries.ToList();
        if (Terms(search).Length == 0) return list;

        var matches = await Task.WhenAll(list.Select(async entry =>
        {
            try
            {
                return Matches(await loader.GetAsync(entry, token), search);
            }
            catch (Exception ex) when (ex is IOException or FormatException)
            {
                // deleted mid-search, or not a readable message
                return false;
            }
        })).WaitAsync(token);

        return list.Where((_, i) => matches[i]).ToList();
    }

    public static string[] Terms(string? search) =>
        (search ?? "").Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    private static string Describe(InternetAddressList? addresses) =>
        addresses == null
            ? ""
            : string.Join('\n', addresses.Mailboxes.Select(m => $"{m.Name} <{m.Address}>"));
}
