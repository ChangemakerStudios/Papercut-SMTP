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
    using System.Collections.Generic;
    using System.Linq;

    using Autofac;

    using Base;

    using Message;
    using MimeKit;

    using Models;

    using Xunit;

    public class LoadMessageFacts : ApiFactBase
    {
        [Fact]
        void should_load_messages_after_send()
        {
            var existedMail = new MimeMessage
            {
                Subject = "Test",
                From = { new MailboxAddress("mffen@gmail.com") }
            };

            Scope.Resolve<MessageRepository>().SaveMessage(fs => existedMail.WriteTo(fs));

            var messages = Get<List<MimeMessageModel>>("/messages");
            Assert.Equal(1, messages.Count);

            var message = messages.First();
            Assert.Equal("Test", message.Subject);
        }
    }
}