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

[McpServerToolType]
public class PapercutMcpTools(
    IMessageRepository messageRepository,
    IMimeMessageLoader messageLoader,
    ILogger logger)
{
    const int MaxRawMessageChars = 200_000;

    const int MaxSectionBytes = 512_000;

    [McpServerTool(Name = "list_messages")]
    [Description("Lists received email messages, newest first. Returns the total message count and a page of message summaries (id, subject, size, created date).")]
    public async Task<GetMessagesResponse> ListMessages(
        [Description("Maximum number of messages to return (default 10)")] int limit = 10,
        [Description("Zero-based offset to start from, for paging (default 0)")] int start = 0,
        CancellationToken cancellationToken = default)
    {
        var messageEntries = messageRepository.LoadMessages().ToList();

        var tasks =
            messageEntries
                .OrderByDescending(msg => msg.ModifiedDate)
                .Skip(start)
                .Take(limit)
                .Select(async e => MimeMessageEntry.RefDto.CreateFrom(new MimeMessageEntry(e, (await messageLoader.GetAsync(e, cancellationToken))!)))
                .ToArray();

        var messages = await Task.WhenAll(tasks).WaitAsync(cancellationToken);

        return new GetMessagesResponse(messageEntries.Count, messages);
    }

    [McpServerTool(Name = "get_message")]
    [Description("Gets the full detail of a received email message by id: from/to/cc/bcc addresses, subject, text and HTML bodies, headers, and attachment sections.")]
    public async Task<MimeMessageEntry.DetailDto> GetMessage(
        [Description("The message id (as returned by list_messages)")] string id,
        CancellationToken cancellationToken = default)
    {
        var messageEntry = this.GetMessageEntry(id);

        return MimeMessageEntry.DetailDto.CreateFrom(
            new MimeMessageEntry(messageEntry, (await messageLoader.GetAsync(messageEntry, cancellationToken))!));
    }

    [McpServerTool(Name = "get_message_raw")]
    [Description("Gets the raw RFC 822 (.eml) source of a received email message by id. Large messages are truncated.")]
    public async Task<string> GetMessageRaw(
        [Description("The message id (as returned by list_messages)")] string id,
        CancellationToken cancellationToken = default)
    {
        var messageEntry = this.GetMessageEntry(id);

        var raw = await File.ReadAllTextAsync(messageEntry.File, cancellationToken);

        if (raw.Length > MaxRawMessageChars)
        {
            raw = raw[..MaxRawMessageChars]
                  + $"{Environment.NewLine}[... truncated at {MaxRawMessageChars:N0} characters -- full message available at GET api/messages/{id}/raw]";
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
        if (index is null && string.IsNullOrEmpty(contentId))
        {
            throw new McpException("Either 'index' or 'contentId' must be provided");
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

        using var memoryStream = new MemoryStream();
        mimePart.Content.DecodeTo(memoryStream);
        var contentBytes = memoryStream.ToArray();

        var result = new SectionContentDto
        {
            Index = sections.IndexOf(mimePart),
            ContentId = mimePart.ContentId,
            FileName = mimePart.FileName,
            MediaType = $"{mimePart.ContentType.MediaType}/{mimePart.ContentType.MediaSubtype}",
            SizeBytes = contentBytes.Length
        };

        if (mimePart is TextPart textPart)
        {
            var text = textPart.Text;

            if (text.Length > MaxRawMessageChars)
            {
                text = text[..MaxRawMessageChars];
                result.Truncated = true;
            }

            result.Text = text;
        }
        else
        {
            if (contentBytes.Length > MaxSectionBytes)
            {
                contentBytes = contentBytes[..MaxSectionBytes];
                result.Truncated = true;
            }

            result.Base64 = Convert.ToBase64String(contentBytes);
        }

        return result;
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

    [McpServerTool(Name = "delete_message")]
    [Description("Deletes a received email message by id.")]
    public string DeleteMessage(
        [Description("The message id (as returned by list_messages)")] string id)
    {
        var messageEntry = this.GetMessageEntry(id);

        try
        {
            messageRepository.DeleteMessage(messageEntry);
        }
        catch (Exception ex)
        {
            logger.Warning(ex, "Failure Deleting Message File {MessageFile}", messageEntry.File);
            throw new McpException($"Failed to delete message '{id}': {ex.Message}");
        }

        return $"Deleted message '{id}'";
    }

    [McpServerTool(Name = "delete_all_messages")]
    [Description("Deletes all received email messages.")]
    public string DeleteAllMessages()
    {
        var deleted = 0;
        var failed = 0;

        foreach (var msg in messageRepository.LoadMessages())
        {
            try
            {
                messageRepository.DeleteMessage(msg);
                deleted++;
            }
            catch (Exception ex)
            {
                logger.Warning(ex, "Failure Deleting Message File {MessageFile}", msg.File);
                failed++;
            }
        }

        return failed == 0
            ? $"Deleted {deleted} message(s)"
            : $"Deleted {deleted} message(s); {failed} failed to delete";
    }

    MessageEntry GetMessageEntry(string id)
    {
        var messageEntry = messageRepository.LoadMessages().FirstOrDefault(msg => msg.Name == id);

        return messageEntry ?? throw new McpException($"Message '{id}' was not found");
    }
}
