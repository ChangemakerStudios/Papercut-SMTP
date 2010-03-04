using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Papercut.Smtp
{
	public class Connection
	{
		public delegate void ProcessData(Connection connection, object data);

		private readonly int _connectionId;
		private Socket _client;
		private DateTime _lastActivity;
		private readonly ProcessData _processData;
		private bool _connected;

		private const int _bufferSize = 64;
		private readonly byte[] _receiveBuffer;
		private StringBuilder _stringBuffer;

		private byte[] _sendBuffer;

		private SmtpSession _session;

		public Connection(int connectionID, Socket client, ProcessData processData)
		{
			// Initialize members
			_connectionId = connectionID;
			_client = client;
			_stringBuffer = new StringBuilder();
			_receiveBuffer = new byte[_bufferSize + 1];
			_processData = processData;
			_connected = true;
			_lastActivity = DateTime.Now;
			_session = new SmtpSession();
			BeginReceive();

			Send("220 {0}", Dns.GetHostName().ToLower());
		}

		public void Close()
		{
			Close(true);
		}

		public void Close(bool triggerEvent)
		{
			// Set our internal flag for no longer connected
			_connected = false;

			// Close out the socket
			if (_client != null && _client.Connected)
			{
				_client.Shutdown(SocketShutdown.Both);
				_client.Close();
			}

			if (triggerEvent)
				OnConnectionClosed(new EventArgs());
			Logger.Write("Connection closed", _connectionId);
		}

		public int ConnectionId
		{
			get { return _connectionId; }
		}

		public Socket Client
		{
			get { return _client; }
			set { _client = value; }
		}

		public bool Connected
		{
			get { return _connected; }
			set { _connected = value; }
		}

		public DateTime LastActivity
		{
			get { return _lastActivity; }
			set { _lastActivity = value; }
		}

		public SmtpSession Session
		{
			get { return _session; }
			set { _session = value; }
		}

		void BeginReceive()
		{
			// Begin to listen for data
			_client.BeginReceive(_receiveBuffer, 0, _bufferSize, SocketFlags.None, new AsyncCallback(ReceiveCallback), this);
		}

		static void ReceiveCallback(IAsyncResult result)
		{
			Connection connection = (Connection)result.AsyncState;

			if (connection == null)
				return;

			try
			{
				// Ensure we're connected... this method gets called when closing a socket with a pending BeginReceive();
				if (!connection._connected)
					return;

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
					string line = connection._stringBuffer.ToString().Replace("\r", "");

					while (line.Contains("\n"))
					{
						// Take a snippet of the buffer, find the line, and process it
						connection._stringBuffer = new StringBuilder(line.Substring(line.IndexOf("\n") + 1));
						line = line.Substring(0, line.IndexOf("\n"));
						Logger.WriteDebug("Received: [" + line + "]", connection._connectionId);
						connection._processData(connection, line);
						line = connection._stringBuffer.ToString();
					}
				}

				// Set up to wait for more
				if (connection._connected)
				{
					try
					{
						connection.BeginReceive();
					}
					catch (ObjectDisposedException) // Socket has been closed.
					{
						return;
					}
				}
			}
			catch (Exception ex)
			{
				Logger.WriteError("Error in Connection.ReceiveCallback", ex, connection._connectionId);
			}
		}

		public IAsyncResult Send(string message)
		{
			_sendBuffer = Encoding.ASCII.GetBytes(message + "\r\n");
			Logger.WriteDebug("Sending: " + message, _connectionId);
			return _client.BeginSend(_sendBuffer, 0, _sendBuffer.Length, SocketFlags.None, new AsyncCallback(SendCallback), this);
		}

		public IAsyncResult Send(string data, params object[] args)
		{
			return Send(string.Format(data, args));
		}

		public void Send(byte[] data)
		{
			_sendBuffer = data;
			Logger.WriteDebug("Sending byte array of " + data.Length + " bytes");
			_client.BeginSend(_sendBuffer, 0, _sendBuffer.Length, SocketFlags.None, new AsyncCallback(SendCallback), this);
		}

		static void SendCallback(IAsyncResult result)
		{
			Connection connection = (Connection)result.AsyncState;

			try
			{
				// Ensure we're connected... this method gets called when closing a socket with a pending BeginReceive();
				if (!connection._connected)
					return;

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

		public event EventHandler ConnectionClosed;
		protected void OnConnectionClosed(EventArgs e)
		{
			if (ConnectionClosed != null)
				ConnectionClosed(this, e);
		}
	}
}