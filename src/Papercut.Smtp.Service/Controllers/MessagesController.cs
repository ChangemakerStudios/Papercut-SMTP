// Papercut
// 
// Copyright © 2008 - 2012 Ken Robertson
// Copyright © 2013 - 2022 Jaben Cargman
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


namespace Papercut.Smtp.Service.Controllers
{
    [ApiController]
    [Route("api/messages")]
    public class MessagesController : Controller
    {
        private readonly CleanupQueue _cleanupQueue;

        readonly MimeMessageLoader _messageLoader;

        readonly MessageRepository _messageRepository;

        public MessagesController(MessageRepository messageRepository, MimeMessageLoader messageLoader, CleanupQueue cleanupQueue)
        {
            this._messageRepository = messageRepository;
            this._messageLoader = messageLoader;
            this._cleanupQueue = cleanupQueue;
        }

        [HttpGet]
        public async Task<ActionResult> GetAll(int limit = 10, int start = 0)
        {
            var messageEntries = this._messageRepository.LoadMessages();

            var messageTasks = messageEntries
                .OrderByDescending(msg => msg.ModifiedDate)
                .Skip(start)
                .Take(limit)
                .Select(async e =>
                    MimeMessageEntry.RefDto.CreateFrom(new MimeMessageEntry(e, await this._messageLoader.GetAsync(e))));

            var messages = await Task.WhenAll(messageTasks);

            return this.Ok(new
            {
                TotalMessageCount = messageEntries.Count,
                Messages = messages
            });
        }

        [HttpDelete]
        public ActionResult DeleteAll()
        {
            this._messageRepository.LoadMessages()
                .ForEach(msg =>
                {
                    try
                    {
                        this._messageRepository.DeleteMessage(msg);
                    }
                    catch
                    {
                        // ignored
                    }
                });

            return this.Ok();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<MimeMessageEntry.DetailDto>> Get(string id)
        {
            var messageEntry = this._messageRepository.LoadMessages().FirstOrDefault(msg => msg.Name == id);
            if (messageEntry == null)
            {
                return this.NotFound();
            }

            var dto = MimeMessageEntry.DetailDto.CreateFrom(new MimeMessageEntry(messageEntry,
                await this._messageLoader.GetAsync(messageEntry)));

            return this.Ok(dto);
        }

        [HttpGet("{messageId}/raw")]
        public ActionResult DownloadRaw(string messageId)
        {
            var messageEntry = this._messageRepository.LoadMessages().FirstOrDefault(msg => msg.Name == messageId);

            if (messageEntry == null)
            {
                return this.NotFound();
            }

            return this.PhysicalFile(messageEntry.File, "message/rfc822", Uri.EscapeDataString(messageId));
        }

        [HttpGet("{messageId}/sections/{index}")]
        public Task<ActionResult> DownloadSection(string messageId, int index)
        {
            return this.DownloadSection(messageId, sections => index >=0 && index < sections.Count ? sections[index] : null);
        }

        [HttpGet("{messageId}/contents/{contentId}")]
        public Task<ActionResult> DownloadSectionContent(string messageId, string contentId)
        {
            return this.DownloadSection(messageId, sections => sections.FirstOrDefault(s => s.ContentId == contentId));
        }

        async Task<ActionResult> DownloadSection(string messageId, Func<List<MimePart>, MimePart> findSection)
        {
            var messageEntry = this._messageRepository.LoadMessages().FirstOrDefault(msg => msg.Name == messageId);
            if (messageEntry == null)
            {
                return this.NotFound();
            }

            var mimeMessage = new MimeMessageEntry(messageEntry, await this._messageLoader.GetAsync(messageEntry));
            var sections = mimeMessage.MailMessage.BodyParts.OfType<MimePart>().ToList();

            var mimePart = findSection(sections);
            if (mimePart == null)
            {
                return this.NotFound();
            }

            var partFile = await new MimeTempFile(mimePart.Content).GetFileAsync();
            var filename = mimePart.FileName ?? mimePart.ContentId ?? Guid.NewGuid().ToString();
            var contentType = $"{mimePart.ContentType.MediaType}/{mimePart.ContentType.MediaSubtype}";

            this._cleanupQueue.EnqueueFile(partFile);

            return this.PhysicalFile(partFile, contentType,
                Uri.EscapeDataString(FileHelper.NormalizeFilename(filename)));
        }
    }
}