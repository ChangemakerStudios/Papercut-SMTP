using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Papercut.Common.Domain;
using Papercut.Core.Domain.Message;
using Papercut.Message;
using Papercut.Message.Helpers;
using Papercut.Service.Web.Models;

namespace Papercut.Service.Web.Notification
{
    public class NewMessageEventHandler: IEventHandler<NewMessageEvent>
    {
        private readonly IHubContext<NewMessagesHub> _hubContext;
        private readonly MimeMessageLoader _messageLoader;
        private readonly ILogger _logger;
        
        public NewMessageEventHandler(IHubContext<NewMessagesHub> hubContext, MimeMessageLoader messageLoader, ILogger<NewMessageEventHandler> logger)
        {
            _hubContext = hubContext;
            _messageLoader = messageLoader;
            _logger = logger;
        }
        
        public void Handle(NewMessageEvent messageEvent)
        {
            var newMessageObj = MimeMessageEntry.RefDto.CreateFrom(
                                    new MimeMessageEntry(messageEvent.NewMessage,
                                    _messageLoader.LoadMailMessage(messageEvent.NewMessage)));
            
            _logger.LogInformation($"New message '{newMessageObj.Id}' has received. Nofifying subscribed clients.");
            _hubContext.Clients.All.SendAsync("new-message-received", newMessageObj);
        }
    }
}