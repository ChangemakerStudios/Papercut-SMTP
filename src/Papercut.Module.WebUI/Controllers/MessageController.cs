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
    using Papercut.Message.Helpers;
    using System;

    public class MessageController : ApiController
    {
        readonly MessageRepository messageRepository;
        readonly MimeMessageLoader messageLoader;

        public MessageController(MessageRepository messageRepository, MimeMessageLoader messageLoader)
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
        public HttpResponseMessage DownloadAttachment(string messageId, string attachmentId)
        {
            var messageEntry = messageRepository.LoadMessages().FirstOrDefault(msg => msg.Name == messageId);
            if (messageEntry == null)
            {
                return Request.CreateResponse(HttpStatusCode.NotFound);
            }

            var mimeMessage = new MimeMessageEntry(messageEntry, messageLoader.LoadMailMessage(messageEntry));
            var mimePart = mimeMessage.MailMessage.BodyParts.FirstOrDefault(e => e.ContentId == attachmentId) as MimePart;
            if (mimePart == null)
            {
                return Request.CreateResponse(HttpStatusCode.NotFound);
            }
            
            var response = new MimePartResponseMessage(Request, mimePart.ContentObject);
            response.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue(DispositionTypeNames.Attachment)
            {
                FileName = Uri.EscapeDataString(FileHelper.NormalizeFilename(mimePart.FileName ?? mimePart.ContentId))
            };
            response.Content.Headers.ContentType = new MediaTypeHeaderValue($"{mimePart.ContentType.MediaType}/{mimePart.ContentType.MediaSubtype}");
            return response;
        }



    }
}