namespace Papercut.Core.Network
{
    using System;
    using System.Net.Sockets;

    using Newtonsoft.Json;

    using Papercut.Core.Events;

    using Serilog;

    public class PapercutClient : IDisposable
    {
        public const string Localhost = "127.0.0.1";

        public const int Port = 37402;

        public PapercutClient(ILogger logger)
        {
            Logger = logger;
            Client = new TcpClient();
        }

        public ILogger Logger { get; private set; }

        public TcpClient Client { get; private set; }

        public void Dispose()
        {
            Client.Close();
        }

        public bool PublishRemoteEvent<TEvent>(TEvent @event) where TEvent : IDomainEvent
        {
            try
            {
                Client.Connect(Localhost, Port);
            }
            catch (SocketException)
            {
                // no listener
                return false;
            }

            string response = Client.ReadString().Trim();
            if (response != "PAPERCUT") return false;

            Logger.Debug("Publishing {@Event} to Socket", @event);

            string json = JsonConvert.SerializeObject(@event);
            Client.WriteFormat("PUBLISH\t{0}\t{1}\r\n", @event.GetType().AssemblyQualifiedName, json.Length);
            Logger.Debug("PUBLISH\t{0:l}\t{1}\r\n", @event.GetType().AssemblyQualifiedName, json.Length);

            response = Client.ReadString().Trim();
            if (response == "READY") Client.WriteString(json);

            Client.Close();

            return true;
        }
    }
}