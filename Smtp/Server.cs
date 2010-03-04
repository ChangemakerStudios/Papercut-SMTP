using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Papercut.Properties;

namespace Papercut.Smtp
{
	public class Server
	{
		IPAddress _address;
		int _port;
		Socket _listener;

		bool _isRunning;
		bool _isStarting;
		bool _isReady;

		Dictionary<int, Connection> _connections = new Dictionary<int, Connection>();
		int connectionID;
		Thread timeoutThread;

		public void Start()
		{
			Logger.Write("Starting server...");

			try
			{
				// Set it as starting
				_isRunning = true;
				_isStarting = true;

				// Start the thread to watch for inactive connections
				if (timeoutThread == null)
				{
					timeoutThread = new Thread(SessionTimeoutWatcher);
					timeoutThread.Start();
				}

				// Create and start new listener socket
				Bind();

				// Set it as ready
				_isReady = true;
			}
			catch (Exception ex)
			{
				Logger.WriteError("Exception thrown in Server.Start()", ex);
				throw;
			}
			finally
			{
				// Done starting
				_isStarting = false;
			}
		}

		public void Stop()
		{
			Logger.Write("Stopping server...");

			try
			{
				// Turn off the running bool
				_isRunning = false;

				// Stop the listener
				_listener.Close();

				// Stop the session timeout thread
				timeoutThread.Join();

				// Close all open connections
				foreach (Connection connection in _connections.Values)
					if (connection != null)
						connection.Close(false);
			}
			catch (Exception ex)
			{
				Logger.WriteError("Exception thrown in Server.Stop()", ex);
			}
		}

		public void Bind()
		{
			try
			{
				// Load IP/Port settings
				if (Settings.Default.IP == "Any")
					_address = IPAddress.Any;
				else
					_address = IPAddress.Parse(Settings.Default.IP);
				_port = Settings.Default.Port;

				// If the listener isn't null, close before rebinding
				if (_listener != null)
					_listener.Close();

				// Bind to the listening port
				_listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
				_listener.Bind(new IPEndPoint(_address, _port));
				_listener.Listen(10);
				_listener.BeginAccept(new AsyncCallback(OnClientAccept), null);
				Logger.Write("Server Ready - Listening for new connections " + _address + ":" + _port + "...");
			}
			catch (Exception ex)
			{
				Logger.WriteError("Exception thrown in Server.Start()", ex);
				throw;
			}
		}

		private void SessionTimeoutWatcher()
		{
			int collectInterval = 5 * 60 - 1;		// How often to collect garbage... every 5 mins
			int statusInterval = 20 * 60 - 1;		// How often to print status... every 20 mins
			int collectCount = 0;
			int statusCount = 0;

			while (_isRunning)
			{
				try
				{
					// Check if the program is up and ready to receive connections
					if (!_isReady)
					{
						// If it is already trying to start, don't have it retry yet
						if (!_isStarting)
							Start();
					}
					else
					{
						// Do garbage collection?
						if (collectCount >= collectInterval)
						{
							// Get the number of current connections
							int[] keys = new int[_connections.Count];
							_connections.Keys.CopyTo(keys, 0);

							// Loop through the connections
							foreach (int key in keys)
							{
								// If they have been idle for too long, disconnect them
								if (DateTime.Now < _connections[key].LastActivity.AddMinutes(20))
								{
									Logger.Write("Session timeout, disconnecting", _connections[key].ConnectionId);
									_connections[key].Close();
								}
							}

							GC.Collect();
							collectCount = 0;
						}
						else
							collectCount++;

						// Print status messages?
						if (statusCount >= statusInterval)
						{
							double memusage = (double)Process.GetCurrentProcess().WorkingSet64 / 1024 / 1024;
							Logger.Write("Current status: " + _connections.Count + " connections, " + memusage.ToString("0.#") + "mb memory used");
							statusCount = 0;
						}
						else
							statusCount++;
					}
				}
				catch (Exception ex)
				{
					Logger.WriteError("Exception occurred in Server.SessionTimeoutWatcher()", ex);
				}

				Thread.Sleep(1000);
			}
		}

		private void OnClientAccept(IAsyncResult ar)
		{
			try
			{
				Socket clientSocket = _listener.EndAccept(ar);
				Interlocked.Increment(ref connectionID);
				Connection connection = new Connection(connectionID, clientSocket, Processor.ProcessCommand);
				connection.ConnectionClosed += connection_ConnectionClosed;
				_connections.Add(connection.ConnectionId, connection);
			}
			catch (ObjectDisposedException)
			{
				// This can occur when stopping the service.  Squash it, it only means the listener was stopped.
				return;
			}
			catch (ArgumentException)
			{
				// This can be thrown when updating settings and rebinding the listener.  It mainly means the IAsyncResult
				// wasn't generated by a BeginAccept event.
				return;
			}
			catch (Exception ex)
			{
				Logger.WriteError("Exception thrown in Server.OnClientAccept", ex);
			}
			finally
			{
				if (_isRunning)
				{
					try
					{
						_listener.BeginAccept(new AsyncCallback(OnClientAccept), null);
					}
					catch
					{
						// This normally happens when trying to rebind to a port that is taken
					}
				}
			}
		}

		void connection_ConnectionClosed(object sender, EventArgs e)
		{
			Connection connection = sender as Connection;
			if (connection == null)
				return;
			_connections.Remove(connection.ConnectionId);
		}

	}
}