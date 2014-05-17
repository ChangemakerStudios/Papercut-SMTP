namespace Papercut.Services
{
    using System;
    using System.Diagnostics;

    using Papercut.Core.Events;
    using Papercut.Core.Network;
    using Papercut.Events;
    using Papercut.Properties;

    using Serilog;

    public class PapercutServiceBackendCoordinator : IHandleEvent<AppPreStartEvent>
    {
        readonly ILogger _logger;

        readonly IPublishEvent _publishEvent;

        readonly PapercutClient _papercutClient;

        readonly SmtpServerCoordinator _smtpServerCoordinator;

        public PapercutServiceBackendCoordinator(ILogger logger, IPublishEvent publishEvent, PapercutClient papercutClient, SmtpServerCoordinator smtpServerCoordinator)
        {
            _logger = logger;
            _publishEvent = publishEvent;
            _papercutClient = papercutClient;
            _smtpServerCoordinator = smtpServerCoordinator;

            _papercutClient.Port = PapercutClient.ServerPort;
        }

        public void Handle(AppPreStartEvent @event)
        {
            try
            {
                var exchangeEvent = new AppProcessExchangeEvent();

                // attempt to connect to the backend server...
                if (!_papercutClient.ExchangeEventServer(ref exchangeEvent)) return;

                // backend server is online...
                _logger.Information("Papercut Backend Service Running. Disabling SMTP in App.");
                _smtpServerCoordinator.SmtpServerEnabled = false;

                if (!string.IsNullOrWhiteSpace(exchangeEvent.MessageWritePath))
                {
                    _logger.Debug(
                        "Background Process Returned {@Event} -- Publishing",
                        exchangeEvent);
                    _publishEvent.Publish(exchangeEvent);
                }
            }
            catch (Exception ex)
            {
                _logger.Warning(ex, "Papercut Backend Service Exception Attempting to Contact");
            }
        }
    }
}