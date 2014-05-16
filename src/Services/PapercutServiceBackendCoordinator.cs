namespace Papercut.Services
{
    using System;

    using Papercut.Core.Events;
    using Papercut.Core.Network;
    using Papercut.Events;

    using Serilog;

    public class PapercutServiceBackendCoordinator : IHandleEvent<AppPreStartEvent>
    {
        readonly ILogger _logger;

        readonly PapercutClient _papercutClient;

        readonly SmtpServerCoordinator _smtpServerCoordinator;

        public PapercutServiceBackendCoordinator(ILogger logger, PapercutClient papercutClient, SmtpServerCoordinator smtpServerCoordinator)
        {
            _logger = logger;
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
                if (_papercutClient.ExchangeEventServer(ref exchangeEvent))
                {
                    // backend server is online...
                    _logger.Information("Papercut Backend Service Running. Disabling SMTP in App.");
                    _smtpServerCoordinator.SmtpServerEnabled = false;

                    _logger.Debug("Background Process Returned {@Event}", exchangeEvent);

                    // add message path to current "watched" paths...
                }
            }
            catch (Exception ex)
            {
                _logger.Warning(ex, "Papercut Backend Service Exception Attempting to Contact");
            }
        }
    }
}