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
    using System.Web.Http;

    using Message;

    using Models;

    using Papercut.Message.Helpers;

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
            var dto = MimeMessageEntry.Dto.CreateFrom(new MimeMessageEntry(messageEntry, messageLoader.LoadMailMessage(messageEntry)));
            return Request.CreateResponse(HttpStatusCode.OK, dto);
        }
    }
}