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

using Papercut.Service.Web;

namespace Papercut.Service.Application.Controllers;

[Route("api/[controller]")]
[McpServerToolType]
[MessageNotFoundExceptionFilter]
public class MessagesController(IMessageRepository messageRepository, IMimeMessageLoader messageLoader, ILogger logger)
    : ControllerBase
{
    [HttpGet]
    [McpServerTool(Name = "list_messages")]
    [Description("Lists received email messages, newest first. Returns the total message count and a page of message summaries (id, subject, size, created date).")]
    public async Task<GetMessagesResponse> GetAll(
        [Description("Maximum number of messages to return (default 10)")] int limit = 10,
        [Description("Zero-based offset to start from, for paging (default 0)")] int start = 0,
        CancellationToken token = default)
    {
        var messageEntries = messageRepository.LoadMessages().ToList();

        var tasks =
            messageEntries
                .OrderByDescending(msg => msg.ModifiedDate)
                .Skip(start)
                .Take(limit)
                .Select(async e => MimeMessageEntry.RefDto.CreateFrom(new MimeMessageEntry(e, (await messageLoader.GetAsync(e, token))!)))
                .ToArray();

        var messages = await Task.WhenAll(tasks).WaitAsync(token);

        return new GetMessagesResponse(messageEntries.Count, messages);
    }

    [HttpDelete]
    [McpServerTool(Name = "delete_all_messages")]
    [Description("Deletes all received email messages.")]
    public string DeleteAll()
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

    [HttpDelete("{id}")]
    [McpServerTool(Name = "delete_message")]
    [Description("Deletes a received email message by id.")]
    public string Delete(
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
            throw new McpException($"Failed to delete message '{id}'");
        }

        return $"Deleted message '{id}'";
    }

    [HttpGet("{id}")]
    [McpServerTool(Name = "get_message")]
    [Description("Gets the full detail of a received email message by id: from/to/cc/bcc addresses, subject, text and HTML bodies, headers, and a manifest of MIME sections (body parts and attachments).")]
    public async Task<MimeMessageEntry.DetailDto> Get(
        [Description("The message id (as returned by list_messages)")] string id,
        CancellationToken token = default)
    {
        var messageEntry = this.GetMessageEntry(id);

        return MimeMessageEntry.DetailDto.CreateFrom(new MimeMessageEntry(messageEntry, (await messageLoader.GetAsync(messageEntry, token))!));
    }

    [HttpGet("{messageId}/raw")]
    public ActionResult DownloadRaw(string messageId)
    {
        var messageEntry = this.GetMessageEntry(messageId);

        var response = new FileStreamResult(System.IO.File.OpenRead(messageEntry.File), "message/rfc822")
                       {
                           FileDownloadName = Uri.EscapeDataString(messageId)
                       };

        return response;
    }

    [HttpGet("{messageId}/sections/{index}")]
    public Task<ActionResult> DownloadSection(string messageId, int index)
    {
        return this.DownloadSection(messageId, sections => index >= 0 && index < sections.Count ? sections[index] : null);
    }

    [HttpGet("{messageId}/contents/{contentId}")]
    public Task<ActionResult> DownloadSectionContent(string messageId, string contentId)
    {
        return this.DownloadSection(messageId, sections => sections.FirstOrDefault(s => s.ContentId == contentId));
    }

    MessageEntry GetMessageEntry(string id)
    {
        var messageEntry = messageRepository.LoadMessages().FirstOrDefault(msg => msg.Name == id);

        return messageEntry ?? throw new MessageNotFoundException(id);
    }

    async Task<ActionResult> DownloadSection(string messageId, Func<List<MimePart>, MimePart?> findSection)
    {
        var messageEntry = this.GetMessageEntry(messageId);

        var mimeMessage = new MimeMessageEntry(messageEntry, (await messageLoader.GetAsync(messageEntry))!);
        var sections = mimeMessage.MailMessage.BodyParts.OfType<MimePart>().ToList();

        var mimePart = findSection(sections);
        if (mimePart == null)
        {
            return this.NotFound();
        }

        var response = new MimePartFileStreamResult(
            mimePart.Content,
            $"{mimePart.ContentType.MediaType}/{mimePart.ContentType.MediaSubtype}");
        var filename = mimePart.FileName ?? mimePart.ContentId ?? Guid.NewGuid().ToString();
        response.FileDownloadName = Uri.EscapeDataString(FileHelper.NormalizeFilename(filename));

        return response;
    }
}