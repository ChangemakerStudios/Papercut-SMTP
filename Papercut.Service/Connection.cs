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

namespace Papercut.Service
{
    #region Using

    using System;
    using System.Net.Sockets;
    using System.Text;
    using System.Threading.Tasks;

    using Papercut.Core;

    #endregion

    /// <summary>
    ///     A connection.
    /// </summary>
    public class Connection : IConnection
    {
        const int BufferSize = 64;

        readonly byte[] _receiveBuffer = new byte[BufferSize + 1];

        byte[] _sendBuffer;

        StringBuilder _stringBuffer = new StringBuilder();

        #region Constructors and Destructors

        public Connection(int id, Socket client, IDataProcessor dataProcessor)
        {
            // Initialize members
            Id = id;
            Client = client;
            DataProcessor = dataProcessor;
            Connected = true;
            LastActivity = DateTime.Now;

            BeginReceive();

            DataProcessor.Begin(this);
        }

        #endregion

        #region Public Events

        public event EventHandler ConnectionClosed;

        #endregion

        #region Public Properties

        public IDataProcessor DataProcessor { get; protected set; }

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

            Logger.Write("Connection closed", Id);
        }
        
        #endregion

        #region Methods

        protected void OnConnectionClosed(EventArgs e)
        {
            if (ConnectionClosed != null) ConnectionClosed(this, e);
        }

        protected void HandleReceive(IAsyncResult result)
        {
            try
            {
                // Receive the rest of the data
                int bytes = Client.EndReceive(result);
                LastActivity = DateTime.Now;

                // Ensure we received bytes
                if (bytes > 0)
                {
                    // Check if the buffer is full of \0, usually means a disconnect
                    if (_receiveBuffer.Length == 64 && _receiveBuffer[0] == '\0')
                    {
                        Close();
                        return;
                    }

                    // Get the string data and append to buffer
                    string data = Encoding.ASCII.GetString(_receiveBuffer, 0, bytes);

                    if ((data.Length == 64) && (data[0] == '\0'))
                    {
                        Close();
                        return;
                    }

                    _stringBuffer.Append(data);

                    // Check if the string buffer contains a line break
                    string line = _stringBuffer.ToString().Replace("\r", string.Empty);

                    while (line.Contains("\n"))
                    {
                        // Take a snippet of the buffer, find the line, and process it
                        _stringBuffer =
                            new StringBuilder(
                                line.Substring(line.IndexOf("\n", StringComparison.Ordinal) + 1));
                        line = line.Substring(0, line.IndexOf("\n"));
                        Logger.WriteDebug(string.Format("Received: [{0}]", line), Id);
                        DataProcessor.Process(line);
                        line = _stringBuffer.ToString();
                    }
                }
                else
                {
                    // nothing received, close and return;
                    Close();
                    return;
                }

                // Set up to wait for more
                if (!Connected) return;

                try
                {
                    BeginReceive();
                }
                catch (ObjectDisposedException)
                {
                    // Socket has been closed.
                }
            }
            catch (Exception ex)
            {
                Logger.WriteError("Error in Connection.ReceiveCallback", ex, Id);
            }
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
                Logger.WriteError("Error in Connection.IsValidConnection", ex, Id);
            }

            return false;
        }

        void BeginReceive()
        {
            // Begin to listen for data
            Client.BeginReceive(
                _receiveBuffer,
                0,
                BufferSize,
                SocketFlags.None,
                result =>
                {
                    if (!IsValidConnection()) return;
                    HandleReceive(result);
                },
                this);
        }

        #endregion
    }
}