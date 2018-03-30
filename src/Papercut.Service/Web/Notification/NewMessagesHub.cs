using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Papercut.Service.Web.Notification
{
    public class NewMessagesHub : Hub
    {
        private readonly ILogger _logger;
        public NewMessagesHub(ILogger<NewMessagesHub> logger)
        {
            _logger = logger;
        }
        
        public override Task OnDisconnectedAsync(Exception exception)
        {
            _logger.LogInformation($"Connection {Context.ConnectionId} lost.");
            return Task.FromResult(0);
        }
 
        public override Task OnConnectedAsync()
        {
            _logger.LogInformation($"New connection {Context.ConnectionId} connected.");
            return Task.FromResult(0);
        }
    }
}