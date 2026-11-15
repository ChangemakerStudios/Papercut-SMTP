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


using System.Net;
using System.Text;
using System.Text.RegularExpressions;

using MimeKit;

namespace Papercut.Message.Index;

/// <summary>
/// What the index keeps of one message: the list-row fields, and the text search looks at.
/// </summary>
public sealed partial record MessageIndexDocument(
    string? Subject,
    IReadOnlyList<IndexedAddress> From,
    string Addresses,
    string Priority,
    int AttachmentCount,
    string AttachmentNames,
    string Body)
{
    /// <summary>Per message. Enough for any token or code in a test email; keeps the index small.</summary>
    public const int MaxBodyChars = 64 * 1024;

    static readonly MessageIndexDocument Unreadable = new(null, [], "", nameof(MessagePriority.Normal), 0, "", "");

    /// <summary>
    /// Reads a message file. Null when it can't be opened right now (still being written,
    /// locked), so it is tried again later; a file that isn't a valid message is indexed
    /// empty rather than retried forever.
    /// </summary>
    public static MessageIndexDocument? Read(string file)
    {
        try
        {
            using var stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);

            // persistent: bodies are read from the file as needed instead of copied into memory
            return Create(MimeMessage.Load(stream, persistent: true));
        }
        catch (IOException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
        catch (Exception ex) when (ex is FormatException or ParseException or InvalidOperationException)
        {
            return Unreadable;
        }
    }

    public static MessageIndexDocument Create(MimeMessage message)
    {
        var addresses = new[] { message.From, message.Sender is { } sender ? new InternetAddressList([sender]) : [], message.ReplyTo, message.To, message.Cc, message.Bcc }
            .SelectMany(list => list.Mailboxes)
            .Select(m => string.IsNullOrEmpty(m.Name) ? m.Address : $"{m.Name} <{m.Address}>");

        var attachments = message.Attachments.ToList();

        return new MessageIndexDocument(
            message.Subject,
            message.From.Mailboxes.Select(m => new IndexedAddress(m.Name, m.Address)).ToList(),
            string.Join('\n', addresses),
            message.Priority.ToString(),
            attachments.Count,
            string.Join('\n', attachments.Select(AttachmentName).Where(n => !string.IsNullOrEmpty(n))),
            BodyText(message));
    }

    static string? AttachmentName(MimeEntity entity) =>
        entity.ContentDisposition?.FileName ?? entity.ContentType?.Name ?? (entity as MimePart)?.FileName;

    /// <summary>The text body, and the HTML body as text: the two often differ (tracking links, footers).</summary>
    static string BodyText(MimeMessage message)
    {
        var body = new StringBuilder();
        var html = message.HtmlBody;

        if (message.TextBody is { Length: > 0 } text)
        {
            body.Append(text);

            // the HTML part is usually the same words again, which would double the
            // index; its link targets are what the text version tends to leave out
            if (!string.IsNullOrEmpty(html))
            {
                foreach (Match link in Hrefs().Matches(html))
                {
                    body.Append('\n').Append(WebUtility.HtmlDecode(link.Groups["url"].Value));
                }
            }
        }
        else if (!string.IsNullOrEmpty(html))
        {
            body.Append(HtmlToText(html));
        }

        return body.Length > MaxBodyChars ? body.ToString(0, MaxBodyChars) : body.ToString();
    }

    public static string HtmlToText(string html)
    {
        var withoutCode = HiddenBlocks().Replace(html, " ");

        // keep link targets: a reset or verify link is often what you are looking for
        var withLinks = Hrefs().Replace(withoutCode, m => $" {m.Groups["url"].Value} ");

        var text = WebUtility.HtmlDecode(Tags().Replace(withLinks, " "));

        return Whitespace().Replace(text, " ").Trim();
    }

    [GeneratedRegex(@"<(script|style|head)\b[^>]*>.*?</\1\s*>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex HiddenBlocks();

    [GeneratedRegex("""<a\b[^>]*?\bhref\s*=\s*["'](?<url>[^"']+)["'][^>]*>""", RegexOptions.IgnoreCase)]
    private static partial Regex Hrefs();

    [GeneratedRegex(@"<[^>]+>")]
    private static partial Regex Tags();

    [GeneratedRegex(@"\s+")]
    private static partial Regex Whitespace();
}
