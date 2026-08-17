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


using System.ComponentModel;

using ModelContextProtocol;
using ModelContextProtocol.Server;

using Papercut.Service.Application.Controllers;

namespace Papercut.Service.Application.Mcp;

/// <summary>
///     MCP-only tools. Tools shared with the REST API live on
///     <see cref="MessagesController" />; these two exist because their REST
///     counterparts return file streams, which cannot cross MCP.
/// </summary>
[McpServerToolType]
public class PapercutMcpTools(
    IMessageRepository messageRepository,
    IMimeMessageLoader messageLoader)
{
    const int MaxRawMessageChars = 200_000;

    const int MaxSectionBytes = 512_000;

    [McpServerTool(Name = "get_message_raw")]
    [Description("Gets the raw RFC 822 (.eml) source of a received email message by id. Large messages are truncated.")]
    public async Task<string> GetMessageRaw(
        [Description("The message id (as returned by list_messages)")] string id,
        CancellationToken cancellationToken = default)
    {
        var messageEntry = this.GetMessageEntry(id);

        using var reader = new StreamReader(messageEntry.File);

        var buffer = new char[MaxRawMessageChars];
        var charsRead = 0;

        while (charsRead < buffer.Length)
        {
            var count = await reader.ReadAsync(buffer.AsMemory(charsRead), cancellationToken);
            if (count == 0) break;
            charsRead += count;
        }

        var raw = new string(buffer, 0, charsRead);

        if (reader.Peek() >= 0)
        {
            raw += $"{Environment.NewLine}[... truncated at {MaxRawMessageChars:N0} characters -- full message available at GET api/messages/{id}/raw]";
        }

        return raw;
    }

    [McpServerTool(Name = "get_message_section")]
    [Description("Gets the decoded content of a single MIME section (body part or attachment) of a message. Sections are listed in the 'sections' manifest returned by get_message. Select by index or contentId. Text sections are returned as text; binary sections as base64. Large sections are truncated.")]
    public async Task<SectionContentDto> GetMessageSection(
        [Description("The message id (as returned by list_messages)")] string id,
        [Description("Zero-based section index from the get_message sections manifest")] int? index = null,
        [Description("Alternative to index: the section's contentId from the manifest")] string? contentId = null,
        CancellationToken cancellationToken = default)
    {
        var hasIndex = index is not null;
        var hasContentId = !string.IsNullOrEmpty(contentId);

        if (hasIndex == hasContentId)
        {
            throw new McpException("Provide exactly one of 'index' or 'contentId'");
        }

        var messageEntry = this.GetMessageEntry(id);

        var mimeMessage = await messageLoader.GetAsync(messageEntry, cancellationToken);
        var sections = mimeMessage!.BodyParts.OfType<MimePart>().ToList();

        var mimePart = index is { } i
            ? i >= 0 && i < sections.Count ? sections[i] : null
            : sections.FirstOrDefault(s => s.ContentId == contentId);

        if (mimePart == null)
        {
            throw new McpException(
                $"Section {(index is null ? $"with contentId '{contentId}'" : $"index {index}")} was not found in message '{id}' ({sections.Count} section(s) available)");
        }

        using var contentStream = mimePart.Content.Open();

        var buffer = new byte[MaxSectionBytes];
        var bytesRead = 0;

        while (bytesRead < buffer.Length)
        {
            var count = await contentStream.ReadAsync(buffer.AsMemory(bytesRead), cancellationToken);
            if (count == 0) break;
            bytesRead += count;
        }

        // drain the remainder without buffering it so SizeBytes reports the true decoded size
        long totalSize = bytesRead;

        if (bytesRead == buffer.Length)
        {
            var drain = new byte[81920];
            int count;
            while ((count = await contentStream.ReadAsync(drain, cancellationToken)) > 0)
            {
                totalSize += count;
            }
        }

        var byteTruncated = totalSize > bytesRead;
        var contentBytes = buffer[..bytesRead];

        var result = new SectionContentDto
        {
            Index = sections.IndexOf(mimePart),
            ContentId = mimePart.ContentId,
            FileName = mimePart.FileName,
            MediaType = $"{mimePart.ContentType.MediaType}/{mimePart.ContentType.MediaSubtype}",
            SizeBytes = totalSize,
            Truncated = byteTruncated
        };

        if (mimePart is TextPart textPart)
        {
            // when fully within bounds, TextPart.Text gives MimeKit's full charset handling;
            // for oversized sections decode only the bounded bytes
            var text = byteTruncated ? GetCharsetEncoding(textPart).GetString(contentBytes) : textPart.Text;

            if (text.Length > MaxRawMessageChars)
            {
                text = text[..MaxRawMessageChars];
                result.Truncated = true;
            }

            result.Text = text;
        }
        else
        {
            result.Base64 = Convert.ToBase64String(contentBytes);
        }

        return result;
    }

    static Encoding GetCharsetEncoding(TextPart textPart)
    {
        try
        {
            return textPart.ContentType.CharsetEncoding ?? Encoding.UTF8;
        }
        catch (Exception)
        {
            return Encoding.UTF8;
        }
    }

    public class SectionContentDto
    {
        public int Index { get; set; }

        public string? ContentId { get; set; }

        public string? FileName { get; set; }

        public string? MediaType { get; set; }

        [Description("Decoded size in bytes")]
        public long SizeBytes { get; set; }

        [Description("Decoded text content; set for text/* sections")]
        public string? Text { get; set; }

        [Description("Base64-encoded content; set for binary sections")]
        public string? Base64 { get; set; }

        [Description("True if the content was truncated; the full section is available at GET api/messages/{id}/sections/{index}")]
        public bool Truncated { get; set; }
    }

    MessageEntry GetMessageEntry(string id)
    {
        var messageEntry = messageRepository.LoadMessages().FirstOrDefault(msg => msg.Name == id);

        return messageEntry ?? throw new MessageNotFoundException(id);
    }
}
