// Papercut
//
// Copyright © 2008 - 2012 Ken Robertson
// Copyright © 2013 - 2017 Jaben Cargman
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


namespace Papercut.Module.WebUI.Controllers
{
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Net.Mime;
    using System.Web.Http;
    using Helpers;
    using Message;
    using MimeKit;
    using Models;
    using Message.Helpers;
    using System;
    using System.Collections.Generic;
    using System.IO;

    using Common.Extensions;

    public class MessagesController : ApiController
    {
        readonly MessageRepository messageRepository;
        readonly MimeMessageLoader messageLoader;

        public MessagesController(MessageRepository messageRepository, MimeMessageLoader messageLoader)
        {
            this.messageRepository = messageRepository;
            this.messageLoader = messageLoader;
        }

        [HttpGet]
        public HttpResponseMessage GetAll(int limit = 10, int start = 0)
        {
            var messageEntries = messageRepository.LoadMessages();

            var messages = messageEntries
                .OrderByDescending(msg => msg.ModifiedDate)
                .Skip(start)
                .Take(limit)
                .Select(e => MimeMessageEntry.RefDto.CreateFrom(new MimeMessageEntry(e, messageLoader.LoadMailMessage(e))))
                .ToList();

            return Request.CreateResponse(HttpStatusCode.OK, new
            {
                TotalMessageCount = messageEntries.Count,
                Messages = messages
            });
        }

        [HttpDelete]
        public HttpResponseMessage DeleteAll()
        {
            messageRepository.LoadMessages()
                .ForEach(msg =>
                {
                    try
                    {
                        messageRepository.DeleteMessage(msg);
                    }catch {}
                });

            return Request.CreateResponse(HttpStatusCode.OK);
        }

        [HttpGet]
        public HttpResponseMessage Get(string id)
        {
            var messageEntry = messageRepository.LoadMessages().FirstOrDefault(msg => msg.Name == id);
            if (messageEntry == null)
            {
                return Request.CreateResponse(HttpStatusCode.NotFound);
            }

            var dto = MimeMessageEntry.DetailDto.CreateFrom(new MimeMessageEntry(messageEntry, messageLoader.LoadMailMessage(messageEntry)));
            return Request.CreateResponse(HttpStatusCode.OK, dto);
        }

        [HttpGet]
        public HttpResponseMessage DownloadRaw(string messageId)
        {
            var messageEntry = messageRepository.LoadMessages().FirstOrDefault(msg => msg.Name == messageId);
            if (messageEntry == null)
            {
                return Request.CreateResponse(HttpStatusCode.NotFound);
            }

            var response = Request.CreateResponse(HttpStatusCode.OK);
            response.Content = new StreamContent(File.OpenRead(messageEntry.File));
            response.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue(DispositionTypeNames.Attachment)
            {
                FileName = Uri.EscapeDataString(messageId)
            };
            response.Content.Headers.ContentType = new MediaTypeHeaderValue("message/rfc822");
            return response;
        }

        [HttpGet]
        public HttpResponseMessage DownloadSection(string messageId, int index)
        {
            return DownloadSection(messageId, sections => (index >=0 && index < sections.Count ? sections[index] : null));
        }

        [HttpGet]
        public HttpResponseMessage DownloadSectionContent(string messageId, string contentId)
        {
            return DownloadSection(messageId, (sections) => sections.FirstOrDefault(s => s.ContentId == contentId));
        }

        HttpResponseMessage DownloadSection(string messageId, Func<List<MimePart>, MimePart> findSection)
        {
            var messageEntry = messageRepository.LoadMessages().FirstOrDefault(msg => msg.Name == messageId);
            if (messageEntry == null)
            {
                return Request.CreateResponse(HttpStatusCode.NotFound);
            }

            var mimeMessage = new MimeMessageEntry(messageEntry, messageLoader.LoadMailMessage(messageEntry));
            var sections = mimeMessage.MailMessage.BodyParts.OfType<MimePart>().ToList();

            var mimePart = findSection(sections);
            if (mimePart == null)
            {
                return Request.CreateResponse(HttpStatusCode.NotFound);
            }

            var response = new MimePartResponseMessage(Request, mimePart.ContentObject);
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