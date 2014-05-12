/*  
 * Papercut
 *
 *  Copyright © 2008 - 2012 Ken Robertson
 *  Copyright © 2013 - 2014 Jaben Cargman
 *  
 *  Licensed under the Apache License, Version 2.0 (the "License");
 *  you may not use this file except in compliance with the License.
 *  You may obtain a copy of the License at
 *  
 *  http://www.apache.org/licenses/LICENSE-2.0
 *  
 *  Unless required by applicable law or agreed to in writing, software
 *  distributed under the License is distributed on an "AS IS" BASIS,
 *  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 *  See the License for the specific language governing permissions and
 *  limitations under the License.
 *  
 */

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