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


namespace Papercut.Service.Application.Messages;

using System.Text.RegularExpressions;

using Common.Helper;

using Domain.Messages;

using Infrastructure;

using Papercut.Message.Helpers;
using Papercut.Rules.App.Relaying;
using Papercut.Rules.Domain.Forwarding;

[Route("api/[controller]")]
[MessageNotFoundExceptionFilter]
public class MessagesController(
    IMessageRepository messageRepository,
    IMimeMessageLoader messageLoader,
    ILogger logger)
    : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<GetMessagesResponse>> GetAll(int limit = 10, int start = 0, string order = "desc", CancellationToken token = default)
    {
        var messageEntries = messageRepository.LoadMessages().ToList();

        if (messageEntries.Count == 0)
        {
            return new GetMessagesResponse(0, []);
        }

        // Generate ETag from the count AND the most recent modified date --
        // count matters because deleting an older message leaves the max
        // modified date (and would otherwise 304 a stale list)
        var latestModifiedDate = messageEntries.Max(msg => msg.ModifiedDate);
        var etag = $"\"{messageEntries.Count}-{latestModifiedDate.Ticks}\"";

        // Check if the client has the same version
        if (Request.Headers.IfNoneMatch.Contains(etag))
        {
            return StatusCode(304);
        }

        // Add ETag to response
        Response.Headers.ETag = etag;

        var ordered = string.Equals(order, "asc", StringComparison.OrdinalIgnoreCase)
            ? messageEntries.OrderBy(msg => msg.ModifiedDate)
            : messageEntries.OrderByDescending(msg => msg.ModifiedDate);

        var tasks =
            ordered
                .Skip(start)
                .Take(limit)
                .Select(async e => RefDto.CreateFrom(new MimeMessageEntry(e, (await messageLoader.GetAsync(e, token))!)))
                .ToArray();

        var messages = await Task.WhenAll(tasks).WaitAsync(token);

        return new GetMessagesResponse(messageEntries.Count, messages);
    }

    [HttpDelete]
    public void DeleteAll()
    {
        foreach (var msg in messageRepository.LoadMessages())
        {
            try
            {
                messageRepository.DeleteMessage(msg);
            }
            catch (Exception ex)
            {
                logger.Warning(ex, "Failure Deleting Message File {MessageFile}", msg.File);
            }
        }
    }

    [HttpDelete("{id}")]
    public ActionResult Delete(string id)
    {
        var messageEntry = this.GetMessageEntry(id);

        try
        {
            messageRepository.DeleteMessage(messageEntry);
        }
        catch (Exception ex)
        {
            logger.Warning(ex, "Failure Deleting Message File {MessageFile}", messageEntry.File);
            return this.StatusCode(500);
        }

        return this.NoContent();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<DetailDto>> Get(string id, CancellationToken token = default)
    {
        var messageEntry = this.GetMessageEntry(id);

        // Generate ETag based on the message's modified date
        var etag = $@"""{messageEntry.ModifiedDate.Ticks}""";

        // Check if client has the same version
        if (Request.Headers.IfNoneMatch.Contains(etag))
        {
            return new StatusCodeResult(304);
        }

        // Add ETag to response
        Response.Headers.ETag = etag;

        return DetailDto.CreateFrom(new MimeMessageEntry(messageEntry, (await messageLoader.GetAsync(messageEntry, token))!));
    }

    // Same permissive check the desktop Forward dialog uses
    static readonly Regex _emailRegex = new(
        @"\A([^@\s]+)@((?:[-a-z0-9]+\.)+[a-z]{2,})\Z",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    [HttpPost("{id}/forward")]
    public async Task<ActionResult> Forward(string id, [FromBody] ForwardMessageRequest request, CancellationToken token = default)
    {
        if (string.IsNullOrWhiteSpace(request.Server)
            || string.IsNullOrWhiteSpace(request.FromEmail)
            || string.IsNullOrWhiteSpace(request.ToEmail))
        {
            return BadRequest(new { error = "Server, FromEmail, and ToEmail are required" });
        }

        if (!_emailRegex.IsMatch(request.FromEmail.Trim()) || !_emailRegex.IsMatch(request.ToEmail.Trim()))
        {
            return BadRequest(new { error = "FromEmail and ToEmail must be valid email addresses" });
        }

        if (request.Port < 1 || request.Port > 65535)
        {
            return BadRequest(new { error = "Port must be between 1 and 65535" });
        }

        var messageEntry = this.GetMessageEntry(id);

        // Same mechanics as the desktop Forward dialog / ForwardRuleDispatch,
        // but sending inline so failures surface to the caller
        var forwardRule = new ForwardRule
        {
            FromEmail = request.FromEmail.Trim(),
            ToEmail = request.ToEmail.Trim(),
            SmtpServer = request.Server.Trim(),
            SmtpPort = request.Port,
            SmtpUseSSL = request.UseSsl,
            SmtpUsername = request.Username?.Trim() ?? string.Empty,
            SmtpPassword = request.Password ?? string.Empty
        };

        var mimeMessage = await messageLoader.GetClonedAsync(messageEntry, token);

        try
        {
            using var client = await forwardRule.CreateConnectedSmtpClientAsync(token);

            forwardRule.PopulateFromRule(mimeMessage);

            await client.SendAsync(mimeMessage, token);
            await client.DisconnectAsync(true, token);
        }
        catch (Exception ex)
        {
            logger.Warning(
                ex,
                "Failed forwarding message {MessageFile} to {SmtpServer}:{SmtpPort}",
                messageEntry.File,
                forwardRule.SmtpServer,
                forwardRule.SmtpPort);

            return Problem(
                title: "Forward failed",
                detail: ex.Message,
                statusCode: StatusCodes.Status502BadGateway);
        }

        return Ok(new { forwarded = id, to = forwardRule.ToEmail });
    }

    /// <summary>
    ///     The message body rendered to display-ready HTML: the most faithful
    ///     alternative part, inline images pointed at this API, and script /
    ///     frame / event-handler content removed. Clients should prefer this
    ///     over the raw HtmlBody on the detail DTO.
    /// </summary>
    [HttpGet("{id}/html")]
    public async Task<ActionResult> GetHtml(string id, CancellationToken token = default)
    {
        var messageEntry = this.GetMessageEntry(id);
        var mimeMessage = (await messageLoader.GetAsync(messageEntry, token))!;

        // relative so the markup keeps working under an HttpPathPrefix
        var contentsBase = $"api/messages/{Uri.EscapeDataString(messageEntry.Id)}/contents";

        var rendered = HtmlMessageRenderer.Render(
            mimeMessage,
            (part, _) =>
            {
                var contentId = part.ContentId?.Trim('<', '>');

                return string.IsNullOrEmpty(contentId)
                    ? null
                    : $"{contentsBase}/{Uri.EscapeDataString(contentId)}";
            });

        return Content(rendered.Html, "text/html; charset=utf-8");
    }

    [HttpGet("{messageId}/raw")]
    public ActionResult DownloadRaw(string messageId)
    {
        var messageEntry = this.GetMessageEntry(messageId);

        var response =
            new FileStreamResult(System.IO.File.OpenRead(messageEntry.File), "message/rfc822")
            {
                FileDownloadName = Uri.EscapeDataString(messageId)
            };

        return response;
    }

    [HttpGet("{messageId}/sections/{index}")]
    public Task<ActionResult> DownloadSection(string messageId, int index)
    {
        return DownloadSection(messageId, sections => index >= 0 && index < sections.Count ? sections[index] : null);
    }

    [HttpGet("{messageId}/contents/{contentId}")]
    public Task<ActionResult> DownloadSectionContent(string messageId, string contentId)
    {
        return DownloadSection(messageId, sections => sections.FirstOrDefault(s => s.ContentId == contentId));
    }

    MessageEntry GetMessageEntry(string id)
    {
        var messageEntry = messageRepository.LoadMessages().FirstOrDefault(msg => msg.Id == id || msg.Name == id);

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
            return NotFound();
        }

        if (!mimePart.ContentMd5.IsSet())
        {
            mimePart.ContentMd5 = mimePart.ComputeContentMd5();
        }

        var etag = $@"""{mimePart.ContentMd5}""";

        // Check if client has the same version
        if (Request.Headers.IfNoneMatch.Contains(etag))
        {
            return new StatusCodeResult(304);
        }

        // Add ETag to response
        Response.Headers.ETag = etag;

        var response = new MimePartFileStreamResult(
            mimePart.Content,
            $"{mimePart.ContentType.MediaType}/{mimePart.ContentType.MediaSubtype}");
        var filename = mimePart.FileName ?? mimePart.ContentId ?? Guid.NewGuid().ToString();
        response.FileDownloadName = Uri.EscapeDataString(FileHelper.NormalizeFilename(filename));

        return response;
    }
}
