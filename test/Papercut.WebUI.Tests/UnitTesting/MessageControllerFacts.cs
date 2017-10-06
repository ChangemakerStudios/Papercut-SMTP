using Autofac;
using MimeKit;
using NUnit.Framework;
using Papercut.Message;
using Papercut.WebUI.Controllers;
using Papercut.WebUI.Models;
using Papercut.WebUI.Test.Base;
using System.Collections.Generic;

namespace Papercut.WebUI.Tests.UnitTesting
{
    public class MessageControllerFacts: TestBase
    {
        readonly MessageRepository _messageRepository;

        public MessageControllerFacts()
        {
            this._messageRepository = Scope.Resolve<MessageRepository>();
        }


        [Test, Order(1)]
        public void should_init_container_and_message_repository()
        {
            Assert.NotNull(this._messageRepository);
        }

        [Test, Order(2)]
        public void should_init_message_controller()
        {

            Assert.NotNull(CreateController());
        }

        [Test, Order(3)]
        public void should_load_messages()
        {
            var existedMail = new MimeMessage
            {
                Subject = "Test",
                From = { new MailboxAddress("mffeng@gmail.com") }
            };
            this._messageRepository.SaveMessage(fs => existedMail.WriteTo(fs));


            var msgCtrl = CreateController();
            var all = msgCtrl.GetAll(10);

            var count = all.AccessProptery<int>("TotalMessageCount");
            var messages = all.AccessProptery<List<MimeMessageEntry.RefDto>>("Messages");

            Assert.AreEqual(1, count);
            Assert.AreEqual(1, messages.Count);
            Assert.AreEqual("Test", messages[0].Subject);
        }

        MessagesController CreateController() {
            var messageLoader = Scope.Resolve<MimeMessageLoader>();
            return new MessagesController(this._messageRepository, messageLoader);
        }
    }
}
