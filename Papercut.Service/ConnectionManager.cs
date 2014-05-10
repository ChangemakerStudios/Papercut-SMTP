namespace Papercut.Service
{
    using System;
    using System.Collections.Concurrent;
    using System.Diagnostics;
    using System.Linq;
    using System.Net.Sockets;
    using System.Threading;

    using Papercut.Core;
    using Papercut.Core.Server;

    public class ConnectionManager : IDisposable
    {
        readonly Func<int, Socket, IProtocol, IConnection> _connectionFactory;

        readonly ConcurrentDictionary<int, IConnection> _connections =
            new ConcurrentDictionary<int, IConnection>();

        readonly Thread _timeoutThread;

        int _connectionID;

        public ConnectionManager(Func<int, Socket, IProtocol, IConnection> connectionFactory)
        {
            _connectionFactory = connectionFactory;
            // Start the thread to watch for inactive connections
            if (_timeoutThread == null)
            {
                _timeoutThread = new Thread(SessionTimeoutWatcher);
                _timeoutThread.Start();
            }
        }

        public void Dispose()
        {
            try
            {
                if (_timeoutThread != null)
                {
                    // Stop the session timeout thread
                    _timeoutThread.Abort();
                    _timeoutThread.Join();
                }
            }
            catch
            {
            }
        }

        public IConnection CreateConnection(Socket clientSocket, IProtocol protocol)
        {
            Interlocked.Increment(ref _connectionID);
            IConnection connection = _connectionFactory(_connectionID, clientSocket, protocol);
            connection.ConnectionClosed += ConnectionClosed;
            _connections.TryAdd(connection.Id, connection);

            return connection;
        }

        public void CloseAll()
        {
            // Close all open connections
            foreach (IConnection connection in
                _connections.Values.Where(connection => connection != null))
            {
                connection.Close(false);
            }
        }

        void ConnectionClosed(object sender, EventArgs e)
        {
            var connection = sender as IConnection;
            if (connection == null) return;

            IConnection noneed;
            _connections.TryRemove(connection.Id, out noneed);
        }

        void SessionTimeoutWatcher()
        {
            int collectInterval = 5 * 60 - 1; // How often to collect garbage... every 5 mins
            int statusInterval = 20 * 60 - 1; // How often to print status... every 20 mins
            int collectCount = 0;
            int statusCount = 0;

            while (Thread.CurrentThread.IsAlive)
            {
                try
                {
                    // Do garbage collection?
                    if (collectCount >= collectInterval)
                    {
                        // Get the number of current connections
                        var keys = new int[_connections.Count];
                        _connections.Keys.CopyTo(keys, 0);

                        // Loop through the connections
                        foreach (int key in keys)
                        {
                            // If they have been idle for too long, disconnect them
                            if (DateTime.Now < _connections[key].LastActivity.AddMinutes(20))
                            {
                                Logger.Write("Session timeout, disconnecting", _connections[key].Id);
                                _connections[key].Close();
                            }
                        }

                        GC.Collect();
                        collectCount = 0;
                    }
                    else collectCount++;

                    // Print status messages?
                    if (statusCount >= statusInterval)
                    {
                        double memusage = (double)Process.GetCurrentProcess().WorkingSet64 / 1024
                                          / 1024;
                        Logger.Write(
                            string.Format(
                                "Current status: {0} connections, {1}mb memory used",
                                _connections.Count,
                                memusage.ToString("0.#")));
                        statusCount = 0;
                    }
                    else statusCount++;
                }
                catch (Exception ex)
                {
                    Logger.WriteError("Exception occurred in Server.SessionTimeoutWatcher()", ex);
                }

                Thread.Sleep(500);
            }
        }
    }
}