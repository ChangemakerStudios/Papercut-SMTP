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
    using Helpers;
    using Message;
    using MimeKit;
    using Models;
    using Message.Helpers;
    using System;
    using System.Collections.Generic;
    using System.IO;

    using Common.Extensions;
    using Microsoft.AspNetCore.Mvc;

    [Route("api/[controller]")]
    public class MessagesController : ControllerBase
    {
        readonly MessageRepository messageRepository;
        readonly MimeMessageLoader messageLoader;

        public MessagesController(MessageRepository messageRepository, MimeMessageLoader messageLoader)
        {
            this.messageRepository = messageRepository;
            this.messageLoader = messageLoader;
        }

        [HttpGet]
        public object GetAll(int limit = 10, int start = 0)
        {
            var messageEntries = messageRepository.LoadMessages();

            var messages = messageEntries
                .OrderByDescending(msg => msg.ModifiedDate)
                .Skip(start)
                .Take(limit)
                .Select(e => MimeMessageEntry.RefDto.CreateFrom(new MimeMessageEntry(e, messageLoader.LoadMailMessage(e))))
                .ToList();

            return new
            {
                TotalMessageCount = messageEntries.Count,
                Messages = messages
            };
        }

        [HttpDelete]
        public void DeleteAll()
        {
            messageRepository.LoadMessages()
                .ForEach(msg =>
                {
                    try
                    {
                        messageRepository.DeleteMessage(msg);
                    }catch {}
                });
        }

        [HttpGet("{id}")]
        public object Get(string id)
        {
            var messageEntry = messageRepository.LoadMessages().FirstOrDefault(msg => msg.Name == id);
            if (messageEntry == null)
            {
                return NotFound();
            }

            var dto = MimeMessageEntry.DetailDto.CreateFrom(new MimeMessageEntry(messageEntry, messageLoader.LoadMailMessage(messageEntry)));
            return dto;
        }

        [HttpGet("{messageId}/raw")]
        public IActionResult DownloadRaw(string messageId)
        {
            var messageEntry = messageRepository.LoadMessages().FirstOrDefault(msg => msg.Name == messageId);
            if (messageEntry == null)
            {
                return NotFound();
            }

            var response = new FileStreamResult(System.IO.File.OpenRead(messageEntry.File), "message/rfc822");
            response.FileDownloadName = Uri.EscapeDataString(messageId);
            return response;
        }

        [HttpGet("{messageId}/sections/{index}")]
        public IActionResult DownloadSection(string messageId, int index)
        {
            return DownloadSection(messageId, sections => (index >=0 && index < sections.Count ? sections[index] : null));
        }

        [HttpGet("{messageId}/contents/{contentId}")]
        public IActionResult DownloadSectionContent(string messageId, string contentId)
        {
            return DownloadSection(messageId, (sections) => sections.FirstOrDefault(s => s.ContentId == contentId));
        }

        IActionResult DownloadSection(string messageId, Func<List<MimePart>, MimePart> findSection)
        {
            var messageEntry = messageRepository.LoadMessages().FirstOrDefault(msg => msg.Name == messageId);
            if (messageEntry == null)
            {
                return NotFound();
            }

            var mimeMessage = new MimeMessageEntry(messageEntry, messageLoader.LoadMailMessage(messageEntry));
            var sections = mimeMessage.MailMessage.BodyParts.OfType<MimePart>().ToList();

            var mimePart = findSection(sections);
            if (mimePart == null)
            {
                return NotFound();
            }

            var response = new MimePartFileStreamResult(mimePart.ContentObject, $"{mimePart.ContentType.MediaType}/{mimePart.ContentType.MediaSubtype}");
            var filename = mimePart.FileName ?? mimePart.ContentId ?? Guid.NewGuid().ToString();
            response.FileDownloadName = Uri.EscapeDataString(FileHelper.NormalizeFilename(filename));
            return response;
        }
               
    }
}