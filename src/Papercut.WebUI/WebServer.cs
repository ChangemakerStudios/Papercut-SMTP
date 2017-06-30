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


namespace Papercut.WebUI
{
    using System;
    using System.Web.Http;
    using System.Web.Http.SelfHost;

    using Common.Domain;

    using Core.Infrastructure.Lifecycle;

    class WebServer : IEventHandler<PapercutServiceReadyEvent>
    {
        const string baseAddress = "http://localhost:6789";

        public void Handle(PapercutServiceReadyEvent @event)
        {
            var configuration = new HttpSelfHostConfiguration(baseAddress);

            configuration.Routes.MapHttpRoute("health", "health", new {controller = "Health"});

            Console.WriteLine("WebUI Server Start ...");

            var server = new HttpSelfHostServer(configuration);

            server.OpenAsync().Wait();
        }
    }
}