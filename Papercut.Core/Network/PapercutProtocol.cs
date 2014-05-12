namespace Papercut.Core.Network
{
    using System;
    using System.IO;
    using System.Net.Sockets;
    using System.Text;

    using Newtonsoft.Json;

    using Papercut.Core.Events;
    using Papercut.Core.Helper;

    using Serilog;

    public class PapercutProtocol : StringCommandProtocol
    {
        readonly IPublishEvent _publishEvent;

        public PapercutProtocol(ILogger logger, IPublishEvent publishEvent)
            : base(logger)
        {
            _publishEvent = publishEvent;
        }

        public Connection Connection { get; protected set; }

        public override void Begin(Connection connection)
        {
            Connection = connection;
            Logger.ForContext("ConnectionId", Connection.Id);
            Connection.SendLine("PAPERCUT");
        }

        protected override void ProcessCommand(string command)
        {
            string[] parts = command.Split('\t');

            switch (parts[0].ToUpper().Trim())
            {
                case "PUBLISH":
                    Type eventType = Type.GetType(parts[1].Trim(), true, true);
                    int size = int.Parse(parts[2].Trim());
                    object @event = ReadEvent(eventType, size);

                    if (@event != null)
                    {
                        Logger.Information("Publishing Received Event {@Event} from Remote", @event);
                        _publishEvent.Publish(eventType, @event);
                    }

                    break;
            }
        }

        object ReadEvent(Type eventType, int size)
        {
            try
            {
                Connection.SendLine("READY").Wait();

                using (Stream networkStream = new NetworkStream(Connection.Client, false))
                {
                    using (var memoryStream = new MemoryStream())
                    {
                        networkStream.CopyBufferedLimited(memoryStream, size);
                        string incoming = new ASCIIEncoding().GetString(memoryStream.ToArray());

                        return JsonConvert.DeserializeObject(incoming, eventType);
                    }
                }
            }
            catch (IOException e)
            {
                Logger.Error(
                    e,
                    "IOException received while reading publish event. Closing this connection.");
                Connection.Close();
            }

            return null;
        }
    }
}