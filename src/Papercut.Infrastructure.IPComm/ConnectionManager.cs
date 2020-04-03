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
    using System.Collections.Concurrent;
    using System.Diagnostics;
    using System.Linq;
    using System.Net.Sockets;
    using System.Reactive.Concurrency;
    using System.Reactive.Disposables;
    using System.Reactive.Linq;
    using System.Threading;

    using Papercut.Infrastructure.IPComm.Protocols;

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
            this.Logger = logger;
            this._connectionFactory = connectionFactory;
        }

        public ILogger Logger { get; set; }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                try
                {
                    this.CloseAll();
                }
                catch (Exception ex)
                {
                    this.Logger.Warning(ex, "Exception Calling CloseAll");
                }

                try
                {
                    this._disposables.Dispose();
                }
                catch (Exception ex)
                {
                    this.Logger.Warning(ex, "Exception Calling Disposable.Dispose");
                }
            }
        }

        public Connection CreateConnection(Socket clientSocket, IProtocol protocol)
        {
            Interlocked.Increment(ref this._connectionID);
            Connection connection = this._connectionFactory(this._connectionID, clientSocket, protocol);
            connection.ConnectionClosed += this.ConnectionClosed;
            this._connections.TryAdd(connection.Id, connection);

            this.Logger.Debug(
                "New Connection {ConnectionId} from {RemoteEndPoint}",
                this._connectionID,
                clientSocket.RemoteEndPoint);

            this.InitCleanupObservables();

            return connection;
        }

        void InitCleanupObservables()
        {
            if (this._isInitialized) return;
            this._isInitialized = true;

            this.Logger.Debug("Initializing Background Processes...");

            this._disposables.Add(
                Observable.Timer(
                    TimeSpan.FromMinutes(1),
                    TimeSpan.FromMinutes(5),
                    TaskPoolScheduler.Default).Subscribe(
                        t =>
                        {
                            // Get the number of current connections
                            int[] keys = this._connections.Keys.ToArray();

                            // Loop through the connections
                            foreach (int key in keys)
                            {
                                // If they have been idle for too long, disconnect them
                                if (DateTime.Now <= this._connections[key].LastActivity.AddMinutes(1)) continue;

                                this.Logger.Information(
                                    "Session timeout, disconnecting {ConnectionId}",
                                    this._connections[key].Id);
                                this._connections[key].Close();
                            }
                        }));

            // print out status every 20 minutes
            double memusage = (double)Process.GetCurrentProcess().WorkingSet64
                              / 1024 / 1024;

            this._disposables.Add(
                Observable.Timer(
                    TimeSpan.FromMinutes(1),
                    TimeSpan.FromMinutes(20),
                    TaskPoolScheduler.Default).Subscribe(
                    t =>
                    {
                        this.Logger.Debug(
                            "Status: {ConnectionCount} Connections {MemoryUsed} Memory Used",
                            this._connections.Count,
                            memusage.ToString("0.#") + "MB");
                    }));
        }

        public void CloseAll()
        {
            // Close all open connections
            foreach (var connection in this._connections.Values.Where(connection => connection != null))
            {
                this._connections.TryRemove(connection.Id, out _);
                connection.Close(false);
            }
        }

        void ConnectionClosed(object sender, EventArgs e)
        {
            if (sender is Connection connection) this._connections.TryRemove(connection.Id, out _);
        }
    }
}