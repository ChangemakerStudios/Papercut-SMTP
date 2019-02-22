// Papercut
// 
// Copyright © 2008 - 2012 Ken Robertson
// Copyright © 2013 - 2019 Jaben Cargman
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

namespace Papercut.Infrastructure.IPComm.IPComm
{
    using System;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading.Tasks;

    using Papercut.Core.Annotations;
    using Papercut.Core.Domain.Network;
    using Papercut.Infrastructure.IPComm.Protocols;

    using Serilog;

    public class PapercutIPCommServer : IServer
    {
        private readonly Func<IProtocol> _protocolFactory;

        IPAddress _address;

        bool _isActive;

        Socket _listener;

        public PapercutIPCommServer(
            Func<PapercutIPCommProtocol> protocolFactory,
            ConnectionManager connectionManager,
            ILogger logger)
        {
            this.ConnectionManager = connectionManager;
            this.Logger = logger;
            this._protocolFactory = protocolFactory;
        }

        public ConnectionManager ConnectionManager { get; set; }

        public ILogger Logger { get; set; }

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

        public string ListenIpAddress
        {
            get => this._address.ToString();
            set
            {
                if (string.IsNullOrWhiteSpace(value) || string.Equals(value, "any", StringComparison.OrdinalIgnoreCase))
                {
                    this._address = IPAddress.Any;
                }
                else
                {
                    this._address = IPAddress.Parse(value);
                }
            }
        }

        public int ListenPort { get; set; }

        public void Stop()
        {
            if (!this.IsActive) return;

            this.Logger.Information("Stopping IPComm Server");

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
                this.Logger.Error(ex, "Exception Stopping IPComm Server");
            }
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        public void Start()
        {
            if (this.IsActive)
            {
                return;
            }

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

        IProtocol GetProtocolInstance()
        {
            return this._protocolFactory();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                try
                {
                    this.Stop();
                    this.CleanupListener();
                    if (this.ConnectionManager != null)
                    {
                        this.ConnectionManager.Dispose();
                        this.ConnectionManager = null;
                    }
                }
                catch (Exception ex)
                {
                    this.Logger.Warning(ex, "Exception Disposing IPComm Server Instance");
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

            this._listener.Bind(new IPEndPoint(this._address, this.ListenPort));
            this._listener.Listen(20);
            this._listener.BeginAccept(this.OnClientAccept, null);

            this.Logger.Information(
                "IPComm Server Ready: Listening for New Connections at {Address}:{ClientPort}",
                this._address,
                this.ListenPort);
        }

        void OnClientAccept([NotNull] IAsyncResult asyncResult)
        {
            if (!this.IsActive || this._listener == null) return;

            try
            {
                Socket clientSocket = this._listener.EndAccept(asyncResult);
                this.ConnectionManager.CreateConnection(clientSocket, this.GetProtocolInstance());
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
                this.Logger.Error(ex, "Exception in IPComm Server Client Accept");
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
    }
}