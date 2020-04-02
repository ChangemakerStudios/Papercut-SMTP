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
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Mime;
    using System.Text;

    using Autofac;

    using MimeKit;

    using NUnit.Framework;

    using Papercut.App.WebApi.Models;
    using Papercut.App.WebApi.Tests.Base;
    using Papercut.Message;

    using ContentType = MimeKit.ContentType;

    public class MessageSectionsFacts : ApiTestBase
    {
        readonly MessageRepository _messageRepository;

        public MessageSectionsFacts()
        {
            this._messageRepository = this._container.Resolve<MessageRepository>();
        }

        [Test, Order(1)]
        public void ShouldLoadDeatailWithSections()
        {
            var existedMail = new MimeMessage
            {
                Body = new Multipart
                {
                    new MimePart(new ContentType("image", "jpeg") {Charset = Encoding.UTF8.EncodingName})
                    {
                        FileName = "sample.pdf",
                        ContentId = Guid.Empty.ToString()
                    }
                }
            };
            this._messageRepository.SaveMessage(existedMail.Subject, fs => existedMail.WriteTo(fs));

            var messageId = this.Get<MessageListResponse>("/api/messages").Messages.First().Id;

            var detail = this.Get<MimeMessageEntry.DetailDto>($"/api/messages/{messageId}");
            Assert.AreEqual(messageId, detail.Id);

            var sections = detail.Sections;
            Assert.AreEqual(1, sections.Count);
            Assert.AreEqual(Guid.Empty.ToString(), sections.First().Id);
            Assert.AreEqual("image/jpeg", sections.First().MediaType);
            Assert.AreEqual("sample.pdf", sections.First().FileName);
        }

        [Test, Order(2)]
        public void ShouldDownloadSectionByIndex()
        {
            var existedMail = new MimeMessage
            {
                Body = new Multipart
                {
                    new MimePart(new ContentType("image", "jpeg") {Charset = Encoding.UTF8.EncodingName})
                    {
                        FileName = "sample.pdf",
                        Content = new MimeContent(
                            new MemoryStream(Encoding.UTF8.GetBytes("Content")), ContentEncoding.Binary)
                    }
                }
            };
            this._messageRepository.SaveMessage(existedMail.Subject, fs => existedMail.WriteTo(fs));

            var messageId = this.Get<MessageListResponse>("/api/messages").Messages.First().Id;

            var response = this.Get($"/api/messages/{messageId}/sections/0");
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

            var disposition = response.Content.Headers.ContentDisposition;
            Assert.AreEqual(DispositionTypeNames.Attachment, disposition.DispositionType);
            Assert.AreEqual("sample.pdf", disposition.FileName);
            Assert.AreEqual("image/jpeg", response.Content.Headers.ContentType.MediaType);
        }

        [Test, Order(3)]
        public void ShouldDownloadSectionByContentId()
        {
            var contentId = Guid.NewGuid().ToString();
            var existedMail = new MimeMessage
            {
                Body = new Multipart
                {
                    new MimePart(new ContentType("image", "jpeg") {Charset = Encoding.UTF8.EncodingName})
                    {
                        FileName = "sample.pdf",
                        ContentId = contentId,
                        Content = new MimeContent(
                            new MemoryStream(Encoding.UTF8.GetBytes("Content")), ContentEncoding.Binary)
                    }
                }
            };
            this._messageRepository.SaveMessage(existedMail.Subject, fs => existedMail.WriteTo(fs));

            var messageId = this.Get<MessageListResponse>("/api/messages").Messages.First().Id;

            var response = this.Get($"/api/messages/{messageId}/contents/{contentId}");
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

            var disposition = response.Content.Headers.ContentDisposition;
            Assert.AreEqual(DispositionTypeNames.Attachment, disposition.DispositionType);
            Assert.AreEqual("sample.pdf", disposition.FileName);
            Assert.AreEqual("image/jpeg", response.Content.Headers.ContentType.MediaType);
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