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

namespace Papercut.SMTP
{
    #region Using

    using System;
    using System.Net;
    using System.Net.Sockets;
    using System.Text;

    #endregion

    /// <summary>
    ///     The connection.
    /// </summary>
    public class Connection
    {
        #region Constants

        /// <summary>
        ///     The _buffer size.
        /// </summary>
        private const int _bufferSize = 64;

        #endregion

        #region Fields

        /// <summary>
        ///     The _connection id.
        /// </summary>
        private readonly int _connectionId;

        /// <summary>
        ///     The _process data.
        /// </summary>
        private readonly ProcessData _processData;

        /// <summary>
        ///     The _receive buffer.
        /// </summary>
        private readonly byte[] _receiveBuffer;

        /// <summary>
        ///     The _client.
        /// </summary>
        private Socket _client;

        /// <summary>
        ///     The _connected.
        /// </summary>
        private bool _connected;

        /// <summary>
        ///     The _last activity.
        /// </summary>
        private DateTime _lastActivity;

        /// <summary>
        ///     The _send buffer.
        /// </summary>
        private byte[] _sendBuffer;

        /// <summary>
        ///     The _string buffer.
        /// </summary>
        private StringBuilder _stringBuffer;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        ///     Initializes a new instance of the <see cref="Connection" /> class.
        /// </summary>
        /// <param name="connectionID">
        ///     The connection id.
        /// </param>
        /// <param name="client">
        ///     The client.
        /// </param>
        /// <param name="processData">
        ///     The process data.
        /// </param>
        public Connection(int connectionID, Socket client, ProcessData processData)
        {
            // Initialize members
            this._connectionId = connectionID;
            this._client = client;
            this._stringBuffer = new StringBuilder();
            this._receiveBuffer = new byte[_bufferSize + 1];
            this._processData = processData;
            this._connected = true;
            this._lastActivity = DateTime.Now;
            this.Session = new SmtpSession();
            this.BeginReceive();

            this.Send("220 {0}", Dns.GetHostName().ToLower());
        }

        #endregion

        #region Delegates

        /// <summary>
        ///     The process data.
        /// </summary>
        /// <param name="connection">
        ///     The connection.
        /// </param>
        /// <param name="data">
        ///     The data.
        /// </param>
        public delegate void ProcessData(Connection connection, object data);

        #endregion

        #region Public Events

        /// <summary>
        ///     The connection closed.
        /// </summary>
        public event EventHandler ConnectionClosed;

        #endregion

        #region Public Properties

        /// <summary>
        ///     Gets or sets Client.
        /// </summary>
        public Socket Client
        {
            get
            {
                return this._client;
            }

            set
            {
                this._client = value;
            }
        }

        /// <summary>
        ///     Gets or sets a value indicating whether Connected.
        /// </summary>
        public bool Connected
        {
            get
            {
                return this._connected;
            }

            set
            {
                this._connected = value;
            }
        }

        /// <summary>
        ///     Gets ConnectionId.
        /// </summary>
        public int ConnectionId
        {
            get
            {
                return this._connectionId;
            }
        }

        /// <summary>
        ///     Gets or sets LastActivity.
        /// </summary>
        public DateTime LastActivity
        {
            get
            {
                return this._lastActivity;
            }

            set
            {
                this._lastActivity = value;
            }
        }

        /// <summary>
        ///     Gets or sets Session.
        /// </summary>
        public SmtpSession Session { get; set; }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        ///     The close.
        /// </summary>
        public void Close()
        {
            this.Close(true);
        }

        /// <summary>
        ///     The close.
        /// </summary>
        /// <param name="triggerEvent">
        ///     The trigger event.
        /// </param>
        public void Close(bool triggerEvent)
        {
            // Set our internal flag for no longer connected
            this._connected = false;

            // Close out the socket
            if (this._client != null && this._client.Connected)
            {
                this._client.Shutdown(SocketShutdown.Both);
                this._client.Close();
            }

            if (triggerEvent)
            {
                this.OnConnectionClosed(new EventArgs());
            }

            Logger.Write("Connection closed", this._connectionId);
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
            this._sendBuffer = Encoding.ASCII.GetBytes(message + "\r\n");
            Logger.WriteDebug("Sending: " + message, this._connectionId);
            return this._client.BeginSend(this._sendBuffer, 0, this._sendBuffer.Length, SocketFlags.None, SendCallback, this);
        }

        /// <summary>
        ///     The send.
        /// </summary>
        /// <param name="data">
        ///     The data.
        /// </param>
        /// <param name="args">
        ///     The args.
        /// </param>
        /// <returns>
        /// </returns>
        public IAsyncResult Send(string data, params object[] args)
        {
            return Send(string.Format(data, args));
        }

        /// <summary>
        ///     The send.
        /// </summary>
        /// <param name="data">
        ///     The data.
        /// </param>
        public void Send(byte[] data)
        {
            this._sendBuffer = data;
            Logger.WriteDebug("Sending byte array of " + data.Length + " bytes");
            this._client.BeginSend(this._sendBuffer, 0, this._sendBuffer.Length, SocketFlags.None, SendCallback, this);
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
            if (this.ConnectionClosed != null)
            {
                this.ConnectionClosed(this, e);
            }
        }

        /// <summary>
        ///     The receive callback.
        /// </summary>
        /// <param name="result">
        ///     The result.
        /// </param>
        private static void ReceiveCallback(IAsyncResult result)
        {
            var connection = (Connection)result.AsyncState;

            if (connection == null)
            {
                return;
            }

            try
            {
                // Ensure we're connected... this method gets called when closing a socket with a pending BeginReceive();
                if (!connection._connected)
                {
                    return;
                }

                // If the socket has been closed, then ensure we close it out
                if (connection._client == null || !connection._client.Connected)
                {
                    connection.Close();
                    return;
                }

                // Receive the rest of the data
                int bytes = connection._client.EndReceive(result);
                connection._lastActivity = DateTime.Now;

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
                        connection._stringBuffer = new StringBuilder(line.Substring(line.IndexOf("\n", StringComparison.Ordinal) + 1));
                        line = line.Substring(0, line.IndexOf("\n"));
                        Logger.WriteDebug(string.Format("Received: [{0}]", line), connection._connectionId);
                        connection._processData(connection, line);
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
                if (!connection._connected)
                {
                    return;
                }

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
                Logger.WriteError("Error in Connection.ReceiveCallback", ex, connection._connectionId);
            }
        }

        /// <summary>
        ///     The send callback.
        /// </summary>
        /// <param name="result">
        ///     The result.
        /// </param>
        private static void SendCallback(IAsyncResult result)
        {
            var connection = (Connection)result.AsyncState;

            try
            {
                // Ensure we're connected... this method gets called when closing a socket with a pending BeginReceive();
                if (!connection._connected)
                {
                    return;
                }

                // If the socket has been closed, then ensure we close it out
                if (connection._client == null || !connection._client.Connected)
                {
                    connection.Close();
                    return;
                }

                connection._client.EndSend(result);
            }
            catch (Exception ex)
            {
                Logger.WriteError("Error in Connection.SendCallback", ex, connection._connectionId);
            }
        }

        /// <summary>
        ///     The begin receive.
        /// </summary>
        private void BeginReceive()
        {
            // Begin to listen for data
            this._client.BeginReceive(this._receiveBuffer, 0, _bufferSize, SocketFlags.None, ReceiveCallback, this);
        }

        #endregion
    }
}