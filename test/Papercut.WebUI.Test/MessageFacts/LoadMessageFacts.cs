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


namespace Papercut.WebUI.Test.MessageFacts
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Http;

    using Autofac;
    using Base;
    using Message;
    using MimeKit;
    using Models;
    using Xunit;

    public class LoadMessageFacts : ApiFactBase
    {
        readonly MessageRepository messageRepository;

        public LoadMessageFacts()
        {
            messageRepository = Scope.Resolve<MessageRepository>();
        }

        [Fact]
        void should_load_all_messages()
        {
            var existedMail = new MimeMessage
            {
                Subject = "Test",
                From = {new MailboxAddress("mffeng@gmail.com")}
            };

            messageRepository.SaveMessage(fs => existedMail.WriteTo(fs));

            var messages = Get<List<MimeMessageEntry.RefDto>>("/api/messages");
            Assert.Equal(1, messages.Count);

            var message = messages.First();
            Assert.NotNull(message.Id);
            Assert.NotNull(message.CreatedAt);
            Assert.NotNull(message.Size);
            Assert.Equal("Test", message.Subject);
        }

        [Fact]
        void should_load_message_detail_by_id()
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
            messageRepository.SaveMessage(fs => existedMail.WriteTo(fs));
            var messages = Get<List<MimeMessageEntry.RefDto>>("/api/messages");
            var id = messages.First().Id;


            var detail = Get<MimeMessageEntry.Dto>($"/api/messages/{id}");
            Assert.Equal(id, detail.Id);
            Assert.NotNull(detail.CreatedAt);

            Assert.Equal(1, detail.From.Count);
            Assert.Equal("mffeng@gmail.com", detail.From.First().Address);


            Assert.Equal(1, detail.To.Count);
            Assert.Equal("xwliu@gmail.com", detail.To.First().Address);


            Assert.Equal(2, detail.Cc.Count);
            Assert.Equal("jjchen@gmail.com", detail.Cc.First().Address);
            Assert.Equal("ygma@gmail.com", detail.Cc.Last().Address);

            Assert.Equal(2, detail.BCc.Count);
            Assert.Equal("rzhe@gmail.com", detail.BCc.First().Address);
            Assert.Equal("xueting@gmail.com", detail.BCc.Last().Address);

            Assert.Equal("Test", detail.Subject);
            Assert.Equal("Hello Buddy", detail.TextBody?.Trim());
            Assert.Null(detail.HtmlBody);
        }

        [Fact]
        void should_return_404_if_not_found_by_id()
        {
            var response = Client.GetAsync("/api/messages/some-strange-id").Result;
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
    }
}