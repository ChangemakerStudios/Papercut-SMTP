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

namespace Papercut.Network.Smtp
{
    using System;
    using System.Net;
    using System.Net.Sockets;

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
            ConnectionManager = connectionManager;

            _serverProtocolType = serverProtocolType;
            Logger = logger.ForContext("ServerProtocolType", _serverProtocolType);
            ProtocolFactory = protocolFactory[_serverProtocolType];
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
                    return _isActive;
                }
            }
            private set
            {
                lock (this)
                {
                    _isActive = value;
                }
            }
        }

        public void Listen(string ip, int port)
        {
            Stop();
            SetEndpoint(ip, port);
            Start();
        }

        public void Stop()
        {
            if (!IsActive) return;

            Logger.Information("Stopping Server {ProtocolType}", _serverProtocolType);

            try
            {
                // Turn off the running bool
                IsActive = false;

                _listener.Close(2);

                ConnectionManager.CloseAll();

                CleanupListener();
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Exception Stopping Server");
            }
        }

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
                    Stop();
                    CleanupListener();
                    if (ConnectionManager != null)
                    {
                        ConnectionManager.Dispose();
                        ConnectionManager = null;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Warning(ex, "Exception Disposing Server Instance");
                }
            }
        }


        protected void CleanupListener()
        {
            if (_listener == null) return;

            _listener.Dispose();
            _listener = null;
        }

        protected void CreateListener()
        {
            // If the listener isn't null, close before rebinding
            CleanupListener();

            // Bind to the listening port
            _listener = new Socket(
                AddressFamily.InterNetwork,
                SocketType.Stream,
                ProtocolType.Tcp);

            _listener.Bind(new IPEndPoint(_address, _port));
            _listener.Listen(20);
            _listener.BeginAccept(OnClientAccept, null);

            Logger.Information(
                "Server Ready - Listening for New Connections {Address}:{ClientPort}",
                _address,
                _port);
        }

        protected void SetEndpoint(string ip, int port)
        {
            // Load IP/ClientPort settings
            if (string.IsNullOrWhiteSpace(ip) ||
                string.Equals(ip, "any", StringComparison.OrdinalIgnoreCase)) _address = IPAddress.Any;
            else _address = IPAddress.Parse(ip);

            _port = port;
        }

        void OnClientAccept([NotNull] IAsyncResult asyncResult)
        {
            if (!IsActive || _listener == null) return;

            try
            {
                Socket clientSocket = _listener.EndAccept(asyncResult);
                ConnectionManager.CreateConnection(clientSocket, ProtocolFactory());
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
                Logger.Error(ex, "Exception in Server.OnClientAccept");
            }
            finally
            {
                if (IsActive)
                {
                    try
                    {
                        _listener.BeginAccept(OnClientAccept, null);
                    }
                    catch
                    {
                        // This normally happens when trying to rebind to a port that is taken
                    }
                }
            }
        }

        void Start()
        {
            Logger.Information("Starting Server {ProtocolType}", _serverProtocolType);

            try
            {
                // Set it as starting
                IsActive = true;

                // Create and start new listener socket
                CreateListener();
            }
            catch
            {
                IsActive = false;
                throw;
            }
        }
    }
}