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


namespace Papercut.WebUI.Controllers
{
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Web.Http;

    using Autofac;

    using Message;

    using Models;

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
        public HttpResponseMessage GetAll()
        {
            var messages = messageRepository.LoadMessages().Select(e => new MimeMessageEntry(e, messageLoader)).ToList();
            return Request.CreateResponse(HttpStatusCode.OK, messages);
        }
    }
}