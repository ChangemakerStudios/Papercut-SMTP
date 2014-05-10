namespace Papercut.Service
{
    using System;

    using Papercut.Core.Network;

    public class PapercutService
    {
        readonly IServer _smtpServer;

        public PapercutService(Func<ServerProtocolType, IServer> serverFactory)
        {
            _smtpServer = serverFactory(ServerProtocolType.Smtp);
        }

        public void Start()
        {
            _smtpServer.Listen(Properties.Settings.Default.IP, Properties.Settings.Default.Port);
        }

        public void Stop()
        {
            _smtpServer.Stop();
        }
    }
}