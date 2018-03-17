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


namespace Papercut.Module.WebUI.Test.MessageFacts
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Mime;
    using System.Text;
    using System.Threading;

    using Autofac;
    using Base;
    using Message;
    using MimeKit;
    using Models;

    using NUnit.Framework;

    using ContentType = MimeKit.ContentType;

    public class MessageMetaFacts : ApiTestBase
    {
        readonly MessageRepository _messageRepository;

        public MessageMetaFacts()
        {
            this._messageRepository = Scope.Resolve<MessageRepository>();
        }

        [Test, Order(1)]
        public void ShouldDeleteAllMessages()
        {
            CreateMessage();
            CreateMessage();

            var response = Delete("/api/messages");

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

            var allMessages = _messageRepository.LoadMessages();
            Assert.AreEqual(0, allMessages.Count);
        }

        [Test, Order(2)]
        public void ShouldDownloadRawMessage()
        {
            var savePath = CreateMessage();
            var messageId = Path.GetFileName(savePath);

            var response = Get($"/api/messages/{messageId}/raw");
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

            var disposition = response.Content.Headers.ContentDisposition;
            Assert.AreEqual(DispositionTypeNames.Attachment, disposition.DispositionType);
            Assert.AreEqual(messageId, disposition.FileName);


            MimeMessage downloadMessage;
            using (var raw = response.Content.ReadAsStreamAsync().Result)
            {
                downloadMessage = MimeMessage.Load(ParserOptions.Default, raw);
            }
            Assert.AreEqual("from@from.com", ((MailboxAddress) downloadMessage.From.First()).Address);
            Assert.AreEqual("to@to.com", ((MailboxAddress)downloadMessage.To.First()).Address);
            Assert.AreEqual("Sample email", downloadMessage.Subject);

            using (var ms = new MemoryStream())
            {
                var bodyContent = (downloadMessage.BodyParts.Single() as MimePart).Content;
                bodyContent.DecodeTo(ms);
                ms.Seek(0, SeekOrigin.Begin);

                Assert.AreEqual("Content example", new StreamReader(ms).ReadToEnd());
            }
        }

        private string CreateMessage()
        {
            var existedMail = new MimeMessage(
                new[] { new MailboxAddress("from@from.com") },
                new[] { new MailboxAddress("to@to.com") },
                 "Sample email",
                 new Multipart
                {
                    new MimePart(new ContentType("text", "html") {Charset = Encoding.UTF8.EncodingName})
                    {
                        Content = new MimeContent(new MemoryStream(Encoding.UTF8.GetBytes("Content example")), ContentEncoding.Binary)
                    }
                });

            return this._messageRepository.SaveMessage(fs => existedMail.WriteTo(fs));
        }

        class MessageListResponse
        {
            public MessageListResponse()
            {
                Messages = new List<MimeMessageEntry.RefDto>();
            }

            public int TotalMessageCount { get; set; }
            public List<MimeMessageEntry.RefDto> Messages { get; set; }
        }
    }
}