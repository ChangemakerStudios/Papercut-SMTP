// Papercut
// 
// Copyright © 2008 - 2012 Ken Robertson
// Copyright © 2013 - 2021 Jaben Cargman
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
// http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.


namespace Papercut.Infrastructure.IPComm
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Sockets;
    using System.Text;
    using System.Threading.Tasks;

    using Papercut.Infrastructure.IPComm.Protocols;

    using Serilog;

    /// <summary>
    ///     A connection.
    /// </summary>
    public class Connection
    {
        private readonly int _bufferSize = 64;

        #region Public Events

        public event EventHandler ConnectionClosed;

        #endregion

        #region Public Methods and Operators

        public void Close(bool triggerEvent = true)
        {
            // Set our internal flag for no longer connected
            this.Connected = false;

            // Close out the socket
            if (this.Client != null && this.Client.Connected)
            {
                this.Client.Shutdown(SocketShutdown.Both);
                this.Client.Close();
            }

            if (triggerEvent) this.OnConnectionClosed(new EventArgs());

            this.Logger.Debug("Connection {ConnectionId} Closed", this.Id);
        }

        #endregion

        #region Constructors and Destructors

        public Connection(int id, Socket client, IProtocol protocol, ILogger logger)
        {
            // Initialize members
            this.Id = id;
            this.Client = client;
            this.Protocol = protocol;
            this.Logger = logger.ForContext("ConnectionId", id);
            this.Connected = true;
            this.Encoding = Encoding.UTF8;
            this.LastActivity = DateTime.Now;
            this.Protocol.BeginAsync(this).Wait();
        }

        #endregion

        #region Public Properties

        public IProtocol Protocol { get; protected set; }

        public ILogger Logger { get; protected set; }

        public Socket Client { get; protected set; }

        public bool Connected { get; protected set; }

        public int Id { get; protected set; }

        public Encoding Encoding { get; protected set; }

        public DateTime LastActivity { get; protected set; }

        #endregion

        #region Methods

        protected void OnConnectionClosed(EventArgs e)
        {
            this.ConnectionClosed?.Invoke(this, e);
        }

        bool IsValidConnection()
        {
            try
            {
                // Ensure we're connected... this method gets called when closing a socket with a pending BeginReceive();
                if (!this.Connected) return false;

                // If the socket has been closed, then ensure we close it out
                if (this.Client == null || !this.Client.Connected)
                {
                    this.Close();
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                this.Logger.Error(ex, "Error in Connection.IsValidConnection");
            }

            return false;
        }

        public async Task<byte[]> ReceiveDataAsync()
        {
            if (!this.IsValidConnection()) return null;

            List<byte[]> incomingArrays = new List<byte[]>();

            var buffer = new byte[this._bufferSize];
            var arraySegment = new ArraySegment<byte>(buffer);

            try
            {
                int received;

                do
                {
                    received = await this.Client.ReceiveAsync(arraySegment, SocketFlags.None);
                    if (received == 0)
                        return null;

                    var incoming = new byte[received];

                    Array.Copy(buffer, arraySegment.Offset, incoming, 0, received);

                    incomingArrays.Add(incoming);
                }
                while (received == this._bufferSize);

                return incomingArrays.SelectMany(bytes => bytes).ToArray();
            }
            catch (ObjectDisposedException)
            {
                // sometimes happens when the socket has already been closed.   
            }

            return null;
        }

        public async Task<int> SendDataAsync(byte[] data)
        {
            if (!this.IsValidConnection()) return 0;

            try
            {
                var arraySegment = new ArraySegment<byte>(data);

                var sentSize = await this.Client.SendAsync(arraySegment, SocketFlags.None);

                if (sentSize != data.Length)
                {
                    this.Logger.Warning(
                        "Data Sent Size ({SentSize} < Data.Size {DataLength}",
                        sentSize,
                        data.Length);
                }
            }
            catch (ObjectDisposedException)
            {
                // sometimes happens when the socket has already been closed.   
            }

            return 0;
        }

        #endregion
    }
}
