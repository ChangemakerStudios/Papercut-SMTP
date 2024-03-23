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


using Papercut.Service.Web;

namespace Papercut.Service.Application.Controllers;

[Route("api/[controller]")]
public class MessagesController : ControllerBase
{
    private readonly ILogger _logger;

    readonly MimeMessageLoader _messageLoader;

    readonly MessageRepository _messageRepository;

    public MessagesController(MessageRepository messageRepository, MimeMessageLoader messageLoader, ILogger logger)
    {
        this._messageRepository = messageRepository;
        this._messageLoader = messageLoader;
        this._logger = logger;
    }

    [HttpGet]
    public async Task<GetMessagesResponse> GetAll(int limit = 10, int start = 0, CancellationToken token = default)
    {
        var messageEntries = this._messageRepository.LoadMessages().ToList();

        var tasks =
            messageEntries
                .OrderByDescending(msg => msg.ModifiedDate)
                .Skip(start)
                .Take(limit)
                .Select(async e => MimeMessageEntry.RefDto.CreateFrom(new MimeMessageEntry(e, (await this._messageLoader.GetAsync(e, token))!)))
                .ToArray();

        var messages = await Task.WhenAll(tasks).WaitAsync(token);

        return new GetMessagesResponse(messageEntries.Count, messages);
    }

    [HttpDelete]
    public void DeleteAll()
    {
        foreach (var msg in this._messageRepository.LoadMessages())
        {
            try
            {
                this._messageRepository.DeleteMessage(msg);
            }
            catch (Exception ex)
            {
                this._logger.Warning(ex, "Failure Deleting Message File {MessageFile}", msg.File);
            }
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<MimeMessageEntry.DetailDto>> Get(string id)
    {
        var messageEntry = this._messageRepository.LoadMessages().FirstOrDefault(msg => msg.Name == id);
        if (messageEntry == null)
        {
            return this.NotFound();
        }

        return MimeMessageEntry.DetailDto.CreateFrom(new MimeMessageEntry(messageEntry, (await this._messageLoader.GetAsync(messageEntry))!));
    }

    [HttpGet("{messageId}/raw")]
    public ActionResult DownloadRaw(string messageId)
    {
        var messageEntry = this._messageRepository.LoadMessages().FirstOrDefault(msg => msg.Name == messageId);
        if (messageEntry == null)
        {
            return this.NotFound();
        }

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

    async Task<ActionResult> DownloadSection(string messageId, Func<List<MimePart>, MimePart?> findSection)
    {
        var messageEntry = this._messageRepository.LoadMessages().FirstOrDefault(msg => msg.Name == messageId);
        if (messageEntry == null)
        {
            return this.NotFound();
        }

        var mimeMessage = new MimeMessageEntry(messageEntry, (await this._messageLoader.GetAsync(messageEntry))!);
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