// Papercut
// 
// Copyright © 2008 - 2012 Ken Robertson
// Copyright © 2013 - 2017 Jaben Cargman
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

namespace Papercut.Network
{
    using System;
    using System.Collections.Concurrent;
    using System.Diagnostics;
    using System.Linq;
    using System.Net.Sockets;
    using System.Reactive.Concurrency;
    using System.Reactive.Disposables;
    using System.Reactive.Linq;
    using System.Threading;

    using Papercut.Core.Domain.Network;

    using Serilog;

    public class ConnectionManager : IDisposable
    {
        readonly Func<int, Socket, IProtocol, Connection> _connectionFactory;

        readonly ConcurrentDictionary<int, Connection> _connections =
            new ConcurrentDictionary<int, Connection>();

        readonly CompositeDisposable _disposables = new CompositeDisposable();

        int _connectionID;

        bool _isInitialized;

        public ConnectionManager(
            Func<int, Socket, IProtocol, Connection> connectionFactory,
            ILogger logger)
        {
            Logger = logger;
            _connectionFactory = connectionFactory;
        }

        public ILogger Logger { get; set; }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                try
                {
                    CloseAll();
                }
                catch (Exception ex)
                {
                    Logger.Warning(ex, "Exception Calling CloseAll");
                }

                try
                {
                    _disposables.Dispose();
                }
                catch (Exception ex)
                {
                    Logger.Warning(ex, "Exception Calling Disposable.Dispose");
                }
            }
        }

        public Connection CreateConnection(Socket clientSocket, IProtocol protocol)
        {
            Interlocked.Increment(ref _connectionID);
            Connection connection = _connectionFactory(_connectionID, clientSocket, protocol);
            connection.ConnectionClosed += ConnectionClosed;
            _connections.TryAdd(connection.Id, connection);

            Logger.Debug(
                "New Connection {ConnectionId} from {RemoteEndPoint}",
                _connectionID,
                clientSocket.RemoteEndPoint);

            InitCleanupObservables();

            return connection;
        }

        void InitCleanupObservables()
        {
            if (_isInitialized) return;
            _isInitialized = true;

            Logger.Debug("Initializing Background Processes...");

            _disposables.Add(
                Observable.Timer(
                    TimeSpan.FromMinutes(1),
                    TimeSpan.FromMinutes(5),
                    TaskPoolScheduler.Default).Subscribe(
                        t =>
                        {
                            // Get the number of current connections
                            int[] keys = _connections.Keys.ToArray();

                            // Loop through the connections
                            foreach (int key in keys)
                            {
                                // If they have been idle for too long, disconnect them
                                if (DateTime.Now > _connections[key].LastActivity.AddMinutes(20))
                                {
                                    Logger.Information(
                                        "Session timeout, disconnecting {ConnectionId}",
                                        _connections[key].Id);
                                    _connections[key].Close();
                                }
                            }
                        }));

            // print out status every 20 minutes
            _disposables.Add(
                Observable.Timer(
                    TimeSpan.FromMinutes(1),
                    TimeSpan.FromMinutes(20),
                    TaskPoolScheduler.Default).Subscribe(
                        t =>
                        {
                            double memusage = (double)Process.GetCurrentProcess().WorkingSet64
                                              / 1024 / 1024;
                            Logger.Debug(
                                "Status: {ConnectionCount} Connections {MemoryUsed} Memory Used",
                                _connections.Count,
                                memusage.ToString("0.#") + "MB");
                        }));
        }

        public void CloseAll()
        {
            // Close all open connections
            foreach (Connection connection in
                _connections.Values.Where(connection => connection != null))
            {
                Connection noneed;
                _connections.TryRemove(connection.Id, out noneed);
                connection.Close(false);
            }
        }

        void ConnectionClosed(object sender, EventArgs e)
        {
            var connection = sender as Connection;
            if (connection == null) return;

            Connection noneed;
            _connections.TryRemove(connection.Id, out noneed);
        }
    }
}