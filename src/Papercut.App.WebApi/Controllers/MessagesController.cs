// Papercut
//
// Copyright © 2008 - 2012 Ken Robertson
// Copyright © 2013 - 2020 Jaben Cargman
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

namespace Papercut.App.WebApi.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Net.Mime;
    using System.Threading.Tasks;
    using System.Web.Http;

    using MimeKit;

    using Papercut.App.WebApi.Helpers;
    using Papercut.App.WebApi.Models;
    using Papercut.Common.Extensions;
    using Papercut.Message;
    using Papercut.Message.Helpers;

    public class MessagesController : ApiController
    {
        readonly MessageRepository _messageRepository;
        readonly MimeMessageLoader _messageLoader;

        public MessagesController(MessageRepository messageRepository, MimeMessageLoader messageLoader)
        {
            this._messageRepository = messageRepository;
            this._messageLoader = messageLoader;
        }

        [HttpGet]
        public async Task<HttpResponseMessage> GetAll(int limit = 10, int start = 0)
        {
            var messageEntries = this._messageRepository.LoadMessages();

            var messageTasks = messageEntries
                .OrderByDescending(msg => msg.ModifiedDate)
                .Skip(start)
                .Take(limit)
                .Select(async e =>
                    MimeMessageEntry.RefDto.CreateFrom(new MimeMessageEntry(e, await this._messageLoader.GetAsync(e))));

            var messages = await Task.WhenAll(messageTasks);

            return this.Request.CreateResponse(HttpStatusCode.OK, new
            {
                TotalMessageCount = messageEntries.Count,
                Messages = messages
            });
        }

        [HttpDelete]
        public HttpResponseMessage DeleteAll()
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

            return this.Request.CreateResponse(HttpStatusCode.OK);
        }

        [HttpGet]
        public async Task<HttpResponseMessage> Get(string id)
        {
            var messageEntry = this._messageRepository.LoadMessages().FirstOrDefault(msg => msg.Name == id);
            if (messageEntry == null)
            {
                return this.Request.CreateResponse(HttpStatusCode.NotFound);
            }

            var dto = MimeMessageEntry.DetailDto.CreateFrom(new MimeMessageEntry(messageEntry, await this._messageLoader.GetAsync(messageEntry)));
            return this.Request.CreateResponse(HttpStatusCode.OK, dto);
        }

        [HttpGet]
        public HttpResponseMessage DownloadRaw(string messageId)
        {
            var messageEntry = this._messageRepository.LoadMessages().FirstOrDefault(msg => msg.Name == messageId);
            if (messageEntry == null)
            {
                return this.Request.CreateResponse(HttpStatusCode.NotFound);
            }

            var response = this.Request.CreateResponse(HttpStatusCode.OK);
            response.Content = new StreamContent(File.OpenRead(messageEntry.File));
            response.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue(DispositionTypeNames.Attachment)
            {
                FileName = Uri.EscapeDataString(messageId)
            };
            response.Content.Headers.ContentType = new MediaTypeHeaderValue("message/rfc822");
            return response;
        }

        [HttpGet]
        public Task<HttpResponseMessage> DownloadSection(string messageId, int index)
        {
            return this.DownloadSection(messageId, sections => (index >=0 && index < sections.Count ? sections[index] : null));
        }

        [HttpGet]
        public Task<HttpResponseMessage> DownloadSectionContent(string messageId, string contentId)
        {
            return this.DownloadSection(messageId, sections => sections.FirstOrDefault(s => s.ContentId == contentId));
        }

        async Task<HttpResponseMessage> DownloadSection(string messageId, Func<List<MimePart>, MimePart> findSection)
        {
            var messageEntry = this._messageRepository.LoadMessages().FirstOrDefault(msg => msg.Name == messageId);
            if (messageEntry == null)
            {
                return this.Request.CreateResponse(HttpStatusCode.NotFound);
            }

            var mimeMessage = new MimeMessageEntry(messageEntry, await this._messageLoader.GetAsync(messageEntry));
            var sections = mimeMessage.MailMessage.BodyParts.OfType<MimePart>().ToList();

            var mimePart = findSection(sections);
            if (mimePart == null)
            {
                return this.Request.CreateResponse(HttpStatusCode.NotFound);
            }

            var response = new MimePartResponseMessage(this.Request, mimePart.Content);
            var filename = mimePart.FileName ?? mimePart.ContentId ?? Guid.NewGuid().ToString();
            response.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue(DispositionTypeNames.Attachment)
            {
                FileName = Uri.EscapeDataString(FileHelper.NormalizeFilename(filename))
            };
            response.Content.Headers.ContentType = new MediaTypeHeaderValue($"{mimePart.ContentType.MediaType}/{mimePart.ContentType.MediaSubtype}");
            return response;
        }
    }
}