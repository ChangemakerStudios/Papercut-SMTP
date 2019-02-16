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
    using System.Net;
    using System.Net.Sockets;
    using System.Threading.Tasks;

    using Autofac.Features.Indexed;

    using Papercut.Core.Annotations;
    using Papercut.Core.Domain.Network;
    using Papercut.Network.Protocols;

    using Serilog;

    public class Server : IServer
    {
        readonly ServerProtocolType _serverProtocolType;

        IPAddress _address;

        bool _isActive;

        Socket _listener;

        int _port;

        public Server(
            ServerProtocolType serverProtocolType,
            IIndex<ServerProtocolType, Func<IProtocol>> protocolFactory,
            ConnectionManager connectionManager,
            ILogger logger)
        {
            this.ConnectionManager = connectionManager;

            this._serverProtocolType = serverProtocolType;
            this.Logger = logger.ForContext("ServerProtocolType", this._serverProtocolType);
            this.ProtocolFactory = protocolFactory[this._serverProtocolType];
        }

        public ConnectionManager ConnectionManager { get; set; }

        public ILogger Logger { get; set; }

        public Func<IProtocol> ProtocolFactory { get; set; }

        public bool IsActive
        {
            get
            {
                lock (this)
                {
                    return this._isActive;
                }
            }
            private set
            {
                lock (this)
                {
                    this._isActive = value;
                }
            }
        }

        public void Listen(string ip, int port)
        {
            this.Stop();
            this.SetEndpoint(ip, port);
            this.Start();
        }

        public async Task Stop()
        {
            await Task.CompletedTask;

            if (!this.IsActive) return;

            this.Logger.Information("Stopping Server {ProtocolType}", this._serverProtocolType);

            try
            {
                // Turn off the running bool
                this.IsActive = false;

                this._listener.Close(2);

                this.ConnectionManager.CloseAll();

                this.CleanupListener();
            }
            catch (Exception ex)
            {
                this.Logger.Error(ex, "Exception Stopping Server");
            }
        }

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
                    this.Stop().Wait();

                    this.CleanupListener();

                    if (this.ConnectionManager != null)
                    {
                        this.ConnectionManager.Dispose();
                        this.ConnectionManager = null;
                    }
                }
                catch (Exception ex)
                {
                    this.Logger.Warning(ex, "Exception Disposing Server Instance");
                }
            }
        }


        protected void CleanupListener()
        {
            if (this._listener == null) return;

            this._listener.Dispose();
            this._listener = null;
        }

        protected void CreateListener()
        {
            // If the listener isn't null, close before rebinding
            this.CleanupListener();

            // Bind to the listening port
            this._listener = new Socket(
                AddressFamily.InterNetwork,
                SocketType.Stream,
                ProtocolType.Tcp);

            this._listener.Bind(new IPEndPoint(this._address, this._port));
            this._listener.Listen(20);
            this._listener.BeginAccept(this.OnClientAccept, null);

            this.Logger.Information(
                "{ProtocolType} Server Ready: Listening for New Connections at {Address}:{ClientPort}",
                this._serverProtocolType,
                this._address,
                this._port);
        }

        protected void SetEndpoint(string ip, int port)
        {
            // Load IP/ClientPort settings
            if (string.IsNullOrWhiteSpace(ip) ||
                string.Equals(ip, "any", StringComparison.OrdinalIgnoreCase)) this._address = IPAddress.Any;
            else this._address = IPAddress.Parse(ip);

            this._port = port;
        }

        void OnClientAccept([NotNull] IAsyncResult asyncResult)
        {
            if (!this.IsActive || this._listener == null) return;

            try
            {
                Socket clientSocket = this._listener.EndAccept(asyncResult);
                this.ConnectionManager.CreateConnection(clientSocket, this.ProtocolFactory());
            }
            catch (ObjectDisposedException)
            {
                // This can occur when stopping the service.  Squash it, it only means the listener was stopped.
            }
            catch (ArgumentException)
            {
                // This can be thrown when updating settings and rebinding the listener.  It mainly means the IAsyncResult
                // wasn't generated by a BeginAccept event.
            }
            catch (Exception ex)
            {
                this.Logger.Error(ex, "Exception in Server.OnClientAccept");
            }
            finally
            {
                if (this.IsActive)
                {
                    try
                    {
                        this._listener.BeginAccept(this.OnClientAccept, null);
                    }
                    catch
                    {
                        // This normally happens when trying to rebind to a port that is taken
                    }
                }
            }
        }

        public async Task Start()
        {
            await Task.CompletedTask;

            try
            {
                // Set it as starting
                this.IsActive = true;

                // Create and start new listener socket
                this.CreateListener();
            }
            catch
            {
                this.IsActive = false;
                throw;
            }
        }
    }
}