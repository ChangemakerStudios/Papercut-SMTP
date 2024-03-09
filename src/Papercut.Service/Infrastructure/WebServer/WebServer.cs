//// Papercut
//// 
//// Copyright © 2008 - 2012 Ken Robertson
//// Copyright © 2013 - 2018 Jaben Cargman
////  
//// Licensed under the Apache License, Version 2.0 (the "License");
//// you may not use this file except in compliance with the License.
//// You may obtain a copy of the License at
////  
//// http://www.apache.org/licenses/LICENSE-2.0
////  
//// Unless required by applicable law or agreed to in writing, software
//// distributed under the License is distributed on an "AS IS" BASIS,
//// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//// See the License for the specific language governing permissions and
//// limitations under the License. 


//namespace Papercut.Service.Infrastructure.WebServer
//{
//    using System;
//    using System.Net.Http;
//    using System.Threading;
//    using System.Threading.Tasks;

//    using Autofac;

//    using Papercut.Common.Domain;
//    using Papercut.Core.Domain.Settings;
//    using Papercut.Core.Infrastructure.Lifecycle;
//    using Papercut.Service;
//    using Papercut.Service.Web.Hosting;

//    using Serilog;

//    public class WebServer : IStartupService, IDisposable
//    {
//        const ushort DefaultHttpPort = 37408;

//        readonly ushort _httpPort;

//        private readonly ILogger _logger;

//        public WebServer(ILifetimeScope scope, ISettingStore settingStore, ILogger logger)
//        {
//            this._logger = logger;
//            PapercutServiceStartup.Scope = scope;
//            this._httpPort = settingStore.Get("HttpPort", DefaultHttpPort);
//        }

//        public void Dispose()
//        {
//            PapercutServiceStartup.Scope = null;
//        }

//        public Task Start(CancellationToken token)
//        {
//            this._logger.Information("Starting Web Server on Port {httpPort}...", this._httpPort);

//            HttpClient client = null;

//            if (this._httpPort <= 0)
//            {
//                var server = PapercutServiceStartup.StartInProcessServer(token);
//                server.Dispose();
//            }
//            else
//            {
//               // WebStartup.Start(this._httpPort, token);
//                //client = new HttpClient { BaseAddress = new Uri($"http://localhost:{this._httpPort}") };
//            }

//            //this._messageBus.Publish(new PapercutWebServerReadyEvent { HttpClient = client });

//            return Task.CompletedTask;
//        }
//    }
//}