namespace Papercut.Services
{
    using System;

    using Papercut.Core.Events;
    using Papercut.Core.Network;
    using Papercut.Events;
    using Papercut.Properties;

    using Serilog;

    public class PapercutServiceBackendCoordinator : IHandleEvent<AppPreStartEvent>,
        IHandleEvent<SettingsUpdatedEvent>
    {
        readonly ILogger _logger;

        readonly Func<PapercutClient> _papercutClientFactory;

        readonly IPublishEvent _publishEvent;

        readonly SmtpServerCoordinator _smtpServerCoordinator;

        public PapercutServiceBackendCoordinator(
            ILogger logger,
            IPublishEvent publishEvent,
            Func<PapercutClient> papercutClientFactory,
            SmtpServerCoordinator smtpServerCoordinator)
        {
            _logger = logger;
            _publishEvent = publishEvent;
            _papercutClientFactory = papercutClientFactory;
            _smtpServerCoordinator = smtpServerCoordinator;
        }

        PapercutClient GetClient()
        {
            var client = _papercutClientFactory();
            client.Port = PapercutClient.ServerPort;
            return client;
        }

        public bool IsBackendServiceOnline { get; private set; }

        public void Handle(AppPreStartEvent @event)
        {
            try
            {
                var exchangeEvent = new AppProcessExchangeEvent();

                // attempt to connect to the backend server...
                using (var client = GetClient())
                {
                    if (!client.ExchangeEventServer(ref exchangeEvent)) return;

                    IsBackendServiceOnline = true;

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
            }
            catch (Exception ex)
            {
                _logger.Warning(ex, "Papercut Backend Service Exception Attempting to Contact");
            }
        }

        public void Handle(SettingsUpdatedEvent @event)
        {
            if (!IsBackendServiceOnline) return;

            try
            {
                using (var client = GetClient())
                {
                    // update the backend service with the new ip/port settings...
                    var smtpServerBindEvent = new SmtpServerBindEvent(
                        Settings.Default.IP,
                        Settings.Default.Port);

                    bool successfulPublish =
                        client.PublishEventServer(smtpServerBindEvent);

                    _logger.Information(
                        successfulPublish
                            ? "Successfully pushed new Smtp Server Binding to Backend Service"
                            : "Papercut Backend Service Failed to Update. Could be offline.");
                }
            }
            catch (Exception ex)
            {
                _logger.Warning(ex, "Papercut Backend Service Exception Attempting to Contact");
            }
        }
    }
}