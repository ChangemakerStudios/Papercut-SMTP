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

    using Papercut.Core;

    #endregion

    /// <summary>
    ///     A connection.
    /// </summary>
    public class Connection : IConnection
    {
        #region Constants

        const int BufferSize = 64;

        #endregion

        #region Fields

        readonly byte[] _receiveBuffer;

        byte[] _sendBuffer;

        StringBuilder _stringBuffer;

        #endregion

        #region Constructors and Destructors

        public Connection(int connectionID, Socket client, IDataProcessor dataProcessor)
        {
            // Initialize members
            ConnectionId = connectionID;
            Client = client;
            _stringBuffer = new StringBuilder();
            _receiveBuffer = new byte[BufferSize + 1];
            DataProcessor = dataProcessor;
            Connected = true;
            LastActivity = DateTime.Now;
            BeginReceive();

            DataProcessor.Begin(this);
        }

        #endregion

        #region Public Events

        /// <summary>
        ///     The connection closed.
        /// </summary>
        public event EventHandler ConnectionClosed;

        #endregion

        #region Public Properties

        /// <summary>
        ///     Gets the data processor
        /// </summary>
        public IDataProcessor DataProcessor { get; protected set; }

        /// <summary>
        ///     Gets or sets Client.
        /// </summary>
        public Socket Client { get; protected set; }

        /// <summary>
        ///     Gets or sets a value indicating whether Connected.
        /// </summary>
        public bool Connected { get; protected set; }

        /// <summary>
        ///     Gets ConnectionId.
        /// </summary>
        public int ConnectionId { get; protected set; }

        /// <summary>
        ///     Gets or sets LastActivity.
        /// </summary>
        public DateTime LastActivity { get; set; }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        ///     The close.
        /// </summary>
        /// <param name="triggerEvent">
        ///     The trigger event.
        /// </param>
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

            Logger.Write("Connection closed", ConnectionId);
        }

        /// <summary>
        ///     The send.
        /// </summary>
        /// <param name="message">
        ///     The message.
        /// </param>
        /// <returns>
        /// </returns>
        public IAsyncResult Send(string message)
        {
            _sendBuffer = Encoding.ASCII.GetBytes(message + "\r\n");
            Logger.WriteDebug("Sending: " + message, ConnectionId);
            return Client.BeginSend(_sendBuffer, 0, _sendBuffer.Length, SocketFlags.None, SendCallback, this);
        }

        /// <summary>
        ///     The send.
        /// </summary>
        /// <param name="data">
        ///     The data.
        /// </param>
        public IAsyncResult Send(byte[] data)
        {
            _sendBuffer = data;
            Logger.WriteDebug("Sending byte array of " + data.Length + " bytes");
            return Client.BeginSend(_sendBuffer, 0, _sendBuffer.Length, SocketFlags.None, SendCallback, this);
        }

        #endregion

        #region Methods

        /// <summary>
        ///     The on connection closed.
        /// </summary>
        /// <param name="e">
        ///     The e.
        /// </param>
        protected void OnConnectionClosed(EventArgs e)
        {
            if (ConnectionClosed != null) ConnectionClosed(this, e);
        }

        /// <summary>
        ///     The receive callback.
        /// </summary>
        /// <param name="result">
        ///     The result.
        /// </param>
        static void ReceiveCallback(IAsyncResult result)
        {
            var connection = (Connection)result.AsyncState;

            if (connection == null) return;

            try
            {
                // Ensure we're connected... this method gets called when closing a socket with a pending BeginReceive();
                if (!connection.Connected) return;

                // If the socket has been closed, then ensure we close it out
                if (connection.Client == null || !connection.Client.Connected)
                {
                    connection.Close();
                    return;
                }

                // Receive the rest of the data
                int bytes = connection.Client.EndReceive(result);
                connection.LastActivity = DateTime.Now;

                // Ensure we received bytes
                if (bytes > 0)
                {
                    // Check if the buffer is full of \0, usually means a disconnect
                    if (connection._receiveBuffer.Length == 64 && connection._receiveBuffer[0] == '\0')
                    {
                        connection.Close();
                        return;
                    }

                    // Get the string data and append to buffer
                    string data = Encoding.ASCII.GetString(connection._receiveBuffer, 0, bytes);

                    if ((data.Length == 64) && (data[0] == '\0'))
                    {
                        connection.Close();
                        return;
                    }

                    connection._stringBuffer.Append(data);

                    // Check if the string buffer contains a line break
                    string line = connection._stringBuffer.ToString().Replace("\r", string.Empty);

                    while (line.Contains("\n"))
                    {
                        // Take a snippet of the buffer, find the line, and process it
                        connection._stringBuffer =
                            new StringBuilder(line.Substring(line.IndexOf("\n", StringComparison.Ordinal) + 1));
                        line = line.Substring(0, line.IndexOf("\n"));
                        Logger.WriteDebug(string.Format("Received: [{0}]", line), connection.ConnectionId);
                        connection.DataProcessor.Process(line);
                        line = connection._stringBuffer.ToString();
                    }
                }
                else
                {
                    // nothing received, close and return;
                    connection.Close();
                    return;
                }

                // Set up to wait for more
                if (!connection.Connected) return;

                try
                {
                    connection.BeginReceive();
                }
                catch (ObjectDisposedException)
                {
                    // Socket has been closed.
                    return;
                }
            }
            catch (Exception ex)
            {
                Logger.WriteError("Error in Connection.ReceiveCallback", ex, connection.ConnectionId);
            }
        }

        /// <summary>
        ///     The send callback.
        /// </summary>
        /// <param name="result">
        ///     The result.
        /// </param>
        static void SendCallback(IAsyncResult result)
        {
            var connection = (Connection)result.AsyncState;

            try
            {
                // Ensure we're connected... this method gets called when closing a socket with a pending BeginReceive();
                if (!connection.Connected) return;

                // If the socket has been closed, then ensure we close it out
                if (connection.Client == null || !connection.Client.Connected)
                {
                    connection.Close();
                    return;
                }

                connection.Client.EndSend(result);
            }
            catch (Exception ex)
            {
                Logger.WriteError("Error in Connection.SendCallback", ex, connection.ConnectionId);
            }
        }

        /// <summary>
        ///     The begin receive.
        /// </summary>
        void BeginReceive()
        {
            // Begin to listen for data
            Client.BeginReceive(_receiveBuffer, 0, BufferSize, SocketFlags.None, ReceiveCallback, this);
        }

        #endregion
    }
}