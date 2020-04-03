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


namespace Papercut.App.WebApi.Tests.MessageFacts
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Threading;

    using Autofac;

    using MimeKit;

    using NUnit.Framework;

    using Papercut.App.WebApi.Models;
    using Papercut.App.WebApi.Tests.Base;
    using Papercut.Message;

    public class LoadMessageFacts : ApiTestBase
    {
        readonly MessageRepository _messageRepository;

        public LoadMessageFacts()
        {
            this._messageRepository = this._container.Resolve<MessageRepository>();
        }

        [Test, Order(0)]
        public void ShouldReturn404IfNotFoundByID()
        {
            var response = this._client.GetAsync("/api/messages/some-strange-id").Result;
            Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Test, Order(1)]
        public void ShouldLoadAllMessages()
        {
            var existedMail = new MimeMessage
            {
                Subject = "Test",
                From = {new MailboxAddress("mffeng@gmail.com")}
            };

            this._messageRepository.SaveMessage(existedMail.Subject, fs => existedMail.WriteTo(fs));

            var messages = this.Get<MessageListResponse>("/api/messages").Messages;
            Assert.AreEqual(1, messages.Count);

            var message = messages.First();
            Assert.NotNull(message.Id);
            Assert.NotNull(message.CreatedAt);
            Assert.NotNull(message.Size);
            Assert.AreEqual("Test", message.Subject);


            // Should serve CreatedAt as UTC value.
            var dateDiff = DateTime.UtcNow - message.CreatedAt.Value;
            Assert.Less(Math.Abs(dateDiff.TotalMinutes), 1);
        }

        [Test, Order(2)]
        public void ShouldLoadMessagesByPagination()
        {
            var existedMail = new MimeMessage
            {
                From = {new MailboxAddress("mffeng@gmail.com")}
            };

            // clear out existing messages
            foreach (var message in this._messageRepository.LoadMessages())
            {
                this._messageRepository.DeleteMessage(message);
            }

            for (int i = 0; i < 10; i++)
            {
                existedMail.Subject = $"Test {i+1}";
                this._messageRepository.SaveMessage(existedMail.Subject, fs => existedMail.WriteTo(fs));
                Thread.Sleep(10);
            }

            var messageResponse = this.Get<MessageListResponse>("/api/messages?limit=2&start=3");
            var messages = messageResponse.Messages;
            Assert.AreEqual(10, messageResponse.TotalMessageCount);
            Assert.AreEqual(2, messages.Count);

            var message1 = messages.First();
            Assert.NotNull(message1.Id);
            Assert.NotNull(message1.CreatedAt);
            Assert.NotNull(message1.Size);
            Assert.AreEqual("Test 7", message1.Subject);

            var message2 = messages.Last();
            Assert.NotNull(message2.Id);
            Assert.NotNull(message2.CreatedAt);
            Assert.NotNull(message2.Size);
            Assert.AreEqual("Test 6", message2.Subject);
        }

        [Test, Order(3)]
        public void ShouldLoadMessageDetailByID()
        {
            var existedMail = new MimeMessage
            {
                Subject = "Test",
                From = {new MailboxAddress("mffeng@gmail.com")},
                To = {new MailboxAddress("xwliu@gmail.com")},
                Cc = {new MailboxAddress("jjchen@gmail.com"), new MailboxAddress("ygma@gmail.com")},
                Bcc = {new MailboxAddress("rzhe@gmail.com"), new MailboxAddress("xueting@gmail.com")},
                Body = new TextPart("plain") {Text = "Hello Buddy"}
            };
            this._messageRepository.SaveMessage(existedMail.Subject, fs => existedMail.WriteTo(fs));
            var messages = this.Get<MessageListResponse>("/api/messages").Messages;
            var id = messages.First().Id;

            var detail = this.Get<MimeMessageEntry.DetailDto>($"/api/messages/{id}");
            Assert.AreEqual(id, detail.Id);
            Assert.NotNull(detail.CreatedAt);

            Assert.AreEqual(1, detail.From.Count);
            Assert.AreEqual("mffeng@gmail.com", detail.From.First().Address);


            Assert.AreEqual(1, detail.To.Count);
            Assert.AreEqual("xwliu@gmail.com", detail.To.First().Address);


            Assert.AreEqual(2, detail.Cc.Count);
            Assert.AreEqual("jjchen@gmail.com", detail.Cc.First().Address);
            Assert.AreEqual("ygma@gmail.com", detail.Cc.Last().Address);

            Assert.AreEqual(2, detail.BCc.Count);
            Assert.AreEqual("rzhe@gmail.com", detail.BCc.First().Address);
            Assert.AreEqual("xueting@gmail.com", detail.BCc.Last().Address);

            Assert.AreEqual("Test", detail.Subject);
            Assert.AreEqual("Hello Buddy", detail.TextBody?.Trim());
            Assert.Null(detail.HtmlBody);
        }

        [Test, Order(4)]
        public void ShouldContainHeadersInMessageDetail()
        {
            var existedMail = new MimeMessage
            {
                Subject = "Test",
                From = {new MailboxAddress("mffeng@gmail.com")},
                To = {new MailboxAddress("xwliu@gmail.com")},
                Body = new TextPart("plain") {Text = "Hello Buddy"}
            };
            existedMail.Headers.Add(HeaderId.ReplyTo, "one@replyto.com");
            existedMail.Headers.Add("X-Extended", "extended value");
            this._messageRepository.SaveMessage(existedMail.Subject, fs => existedMail.WriteTo(fs));
            var messages = this.Get<MessageListResponse>("/api/messages").Messages;
            var id = messages.First().Id;


            var detail = this.Get<MimeMessageEntry.DetailDto>($"/api/messages/{id}");
            Assert.AreEqual(id, detail.Id);

            var headers = detail.Headers;
            Assert.AreEqual("one@replyto.com",  headers.First(h => h.Name == "Reply-To").Value);
            Assert.AreEqual("extended value", headers.First(h => h.Name == "X-Extended").Value);
        }

        class MessageListResponse
        {
            public MessageListResponse()
            {
                this.Messages = new List<MimeMessageEntry.RefDto>();
            }

            public int TotalMessageCount { get; set; }
            public List<MimeMessageEntry.RefDto> Messages { get; set; }
        }
    }
}