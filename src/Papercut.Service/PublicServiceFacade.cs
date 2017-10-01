

namespace Papercut.Service
{
    using Serilog;
    using Papercut.Common.Domain;
    using Papercut.Core.Domain.Message;
    using Papercut.WebUI;
    using System;
    using System.Net.Http;

    public class PublicServiceFacade : IEventHandler<WebUIServerReadyEvent>, IEventHandler<NewMessageEvent>
    {
        public HttpClient PapercutWebClient { get; set; }
        public event EventHandler<NewMessageEvent> NewMessageReceived;
        private readonly ILogger _logger;

        public PublicServiceFacade(ILogger logger) {
            _logger = logger;
        }

        public void Handle(WebUIServerReadyEvent readyEvent)
        {
            PapercutWebClient = readyEvent.InProcessServer?.CreateClient();
        }

        public void Handle(NewMessageEvent @event)
        {
            try
            {
                NewMessageReceived?.Invoke(this, @event);
            }
            catch (Exception ex) {
                _logger.Error(ex, "Error when executing NewMessageReceived event");
            }
        }
    }
}