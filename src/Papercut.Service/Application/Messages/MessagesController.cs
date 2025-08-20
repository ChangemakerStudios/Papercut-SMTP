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


namespace Papercut.Service.Application.Messages;

using Common.Helper;

using Domain.Messages;

using Infrastructure;

[Route("api/[controller]")]
public class MessagesController(
    MessageRepository messageRepository,
    MimeMessageLoader messageLoader,
    ILogger logger)
    : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<GetMessagesResponse>> GetAll(int limit = 10, int start = 0, CancellationToken token = default)
    {
        var messageEntries = messageRepository.LoadMessages().ToList();

        if (messageEntries.Count == 0)
        {
            return new GetMessagesResponse(0, []);
        }
        
        // Generate ETag based on the most recent modified date
        var latestModifiedDate = messageEntries.Max(msg => msg.ModifiedDate);
        var etag = $"\"{latestModifiedDate.Ticks}\"";
        
        // Check if the client has the same version
        if (Request.Headers.IfNoneMatch.Contains(etag))
        {
            return StatusCode(304);
        }
        
        // Add ETag to response
        Response.Headers.ETag = etag;

        var tasks =
            messageEntries
                .OrderByDescending(msg => msg.ModifiedDate)
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

    [HttpGet("{id}")]
    public async Task<ActionResult<DetailDto>> Get(string id)
    {
        var messageEntry = messageRepository.LoadMessages().FirstOrDefault(msg => msg.Id == id || msg.Name == id);
        if (messageEntry == null)
        {
            return NotFound();
        }

        // Generate ETag based on the message's modified date
        var etag = $@"""{messageEntry.ModifiedDate.Ticks}""";
        
        // Check if client has the same version
        if (Request.Headers.IfNoneMatch.Contains(etag))
        {
            return new StatusCodeResult(304);
        }
        
        // Add ETag to response
        Response.Headers.ETag = etag;

        return DetailDto.CreateFrom(new MimeMessageEntry(messageEntry, (await messageLoader.GetAsync(messageEntry))!));
    }

    [HttpGet("{messageId}/raw")]
    public ActionResult DownloadRaw(string messageId)
    {
        var messageEntry = messageRepository.LoadMessages().FirstOrDefault(msg => msg.Id == messageId || msg.Name == messageId);
        if (messageEntry == null)
        {
            return NotFound();
        }

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

    async Task<ActionResult> DownloadSection(string messageId, Func<List<MimePart>, MimePart?> findSection)
    {
        var messageEntry = messageRepository.LoadMessages().FirstOrDefault(msg => msg.Id == messageId || msg.Name == messageId);
        if (messageEntry == null)
        {
            return NotFound();
        }

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
