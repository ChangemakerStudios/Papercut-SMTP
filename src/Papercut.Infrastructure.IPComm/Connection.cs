// Papercut
// 
// Copyright © 2008 - 2012 Ken Robertson
// Copyright © 2013 - 2020 Jaben Cargman
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
    using System.Net.Sockets;
    using System.Text;
    using System.Threading.Tasks;

    using Papercut.Common.Helper;
    using Papercut.Core.Annotations;
    using Papercut.Infrastructure.IPComm.Protocols;

    using Serilog;

    /// <summary>
    ///     A connection.
    /// </summary>
    public class Connection
    {
        const int BufferSize = 64;

        readonly byte[] _receiveBuffer = new byte[BufferSize + 1];

        #region Constructors and Destructors

        public Connection(int id, Socket client, IProtocol protocol, ILogger logger)
        {
            // Initialize members
            this.Id = id;
            this.Client = client;
            this.Protocol = protocol;
            this.Logger = logger;

            this.Logger.ForContext("ConnectionId", id);

            this.Connected = true;
            this.LastActivity = DateTime.Now;

            this.BeginReceive();

            this.Protocol.Begin(this);
        }

        #endregion

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

        #region Public Properties

        public IProtocol Protocol { get; protected set; }

        public ILogger Logger { get; set; }

        public Socket Client { get; protected set; }

        public bool Connected { get; protected set; }

        public int Id { get; protected set; }

        public DateTime LastActivity { get; set; }
        
        public Encoding Encoding { get; set; } = Encoding.UTF8;

        #endregion

        #region Methods

        protected void OnConnectionClosed(EventArgs e)
        {
            this.ConnectionClosed?.Invoke(this, e);
        }

        protected bool ContinueProcessReceive([NotNull] IAsyncResult result)
        {
            if (result == null) throw new ArgumentNullException(nameof(result));

            SocketError socketError = SocketError.Success;

            try
            {
                // Receive the rest of the data
                int sizeReceived = this.Client.EndReceive(result, out socketError);

                this.LastActivity = DateTime.Now;

                if (socketError != SocketError.Success)
                {
                    this.Logger.Warning("Socket Error Ending Receive {SocketError}", socketError);
                    this.Close();

                    return false;
                }

                // Ensure we received bytes
                if (sizeReceived <= 0 || (this._receiveBuffer.Length == 64 && this._receiveBuffer[0] == '\0'))
                {
                    // nothing received, close and return;
                    this.Close();
                    return false;
                }

                var incoming = new byte[sizeReceived];
                Array.Copy(this._receiveBuffer, incoming, sizeReceived);

                this.Protocol.ProcessIncomingBuffer(incoming, this.Encoding);

                // continue receiving...
                return true;
            }
            catch (Exception exception)
            {
                this.Logger.Warning(exception, "Failed to End Receive on Async Socket {SocketError}", socketError);
            }

            return false;
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

        void BeginReceive()
        {
            try
            {
                // Begin to listen for data
                this.Client.BeginReceive(
                    this._receiveBuffer,
                    0,
                    BufferSize,
                    SocketFlags.None,
                    result =>
                    {
                        if (this.IsValidConnection() && this.ContinueProcessReceive(result))
                        {
                            if (this.Connected && this.Client.Connected)
                            {
                                // continue processing
                                this.BeginReceive();
                            }
                        }
                    },
                    this);
            }
            catch (Exception ex)
            {
                this.Logger.Error(ex, "Error in Connection.BeginReceive");
            }
        }

        public async Task<int> SendData(byte[] data)
        {
            if (!this.Connected || !this.Client.Connected) return 0;

            // Use overload that takes an IAsyncResult directly
            try
            {
                void NullOp(IAsyncResult i)
                {
                }

                IAsyncResult result = this.Client.BeginSend(data, 0, data.Length, SocketFlags.None, NullOp, null);

                if (result != null && this.Client.Connected)
                {
                    await Task.Factory.FromAsync(result, r =>
                    {
                        if (this.Client.Connected)
                        {
                            var sendResult = this.Client.EndSend(r, out var socketError);

                            if (socketError != SocketError.Success)
                            {
                                this.Logger.Warning("Socket Send {SocketError}", socketError);
                            }

                            return sendResult;
                        }

                        return 0;
                    });
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
