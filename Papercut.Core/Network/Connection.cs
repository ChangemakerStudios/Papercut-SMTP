// Papercut
// 
// Copyright © 2008 - 2012 Ken Robertson
// Copyright © 2013 - 2015 Jaben Cargman
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

namespace Papercut.Core.Network
{
    #region Using

    using System;
    using System.Net.Sockets;

    using Papercut.Core.Annotations;

    using Serilog;

    #endregion

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
            Id = id;
            Client = client;
            Protocol = protocol;
            Logger = logger;

            Logger.ForContext("ConnectionId", id);

            Connected = true;
            LastActivity = DateTime.Now;

            BeginReceive();

            Protocol.Begin(this);
        }

        #endregion

        #region Public Events

        public event EventHandler ConnectionClosed;

        #endregion

        #region Public Properties

        public IProtocol Protocol { get; protected set; }

        public ILogger Logger { get; set; }

        public Socket Client { get; protected set; }

        public bool Connected { get; protected set; }

        public int Id { get; protected set; }

        public DateTime LastActivity { get; set; }

        #endregion

        #region Public Methods and Operators

        public void Close(bool triggerEvent = true)
        {
            // Set our internal flag for no longer connected
            Connected = false;

            // Close out the socket
            if (Client != null && Client.Connected)
            {
                Client.Shutdown(SocketShutdown.Both);
                Client.Close();
            }

            if (triggerEvent) OnConnectionClosed(new EventArgs());

            Logger.Debug("Connection {ConnectionId} Closed", Id);
        }

        #endregion

        #region Methods

        protected void OnConnectionClosed(EventArgs e)
        {
            if (ConnectionClosed != null) ConnectionClosed(this, e);
        }

        protected bool ContinueProcessReceive([NotNull] IAsyncResult result)
        {
            if (result == null) throw new ArgumentNullException("result");

            try
            {
                // Receive the rest of the data
                int bytes = Client.EndReceive(result);
                LastActivity = DateTime.Now;

                // Ensure we received bytes
                if (bytes <= 0 || (_receiveBuffer.Length == 64 && _receiveBuffer[0] == '\0'))
                {
                    // nothing received, close and return;
                    Close();
                    return false;
                }

                var incoming = new byte[bytes];
                Array.Copy(_receiveBuffer, incoming, bytes);
                Protocol.ProcessIncomingBuffer(incoming);

                // continue receiving...
                return true;
            }
            catch (Exception ex)
            {
                Logger.Warning(ex, "Failed to End Receive on Async Socket");
            }

            return false;
        }

        bool IsValidConnection()
        {
            try
            {
                // Ensure we're connected... this method gets called when closing a socket with a pending BeginReceive();
                if (!Connected) return false;

                // If the socket has been closed, then ensure we close it out
                if (Client == null || !Client.Connected)
                {
                    Close();
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error in Connection.IsValidConnection");
            }

            return false;
        }

        void BeginReceive()
        {
            try
            {
                // Begin to listen for data
                Client.BeginReceive(
                    _receiveBuffer,
                    0,
                    BufferSize,
                    SocketFlags.None,
                    result =>
                    {
                        if (IsValidConnection() && ContinueProcessReceive(result))
                        {
                            if (Connected && Client.Connected)
                            {
                                // continue processing
                                BeginReceive();
                            }
                        }
                    },
                    this);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error in Connection.BeginReceive");
            }
        }

        #endregion
    }
}