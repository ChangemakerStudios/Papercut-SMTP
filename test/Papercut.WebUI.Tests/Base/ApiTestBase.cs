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


namespace Papercut.WebUI.Test.Base
{
    using System;
    using System.Net.Http;
    using Autofac;

    using Core.Domain.Application;
    using Core.Domain.Paths;
    using Core.Infrastructure.Container;

    using WebServerFacts;
    using System.Reflection;
    using Newtonsoft.Json;
    using System.Threading;
    using Papercut.Service.Web;
    using Papercut.Service.Web.Hosting;
    using Papercut.Core.Domain.Paths;
    using System.IO;

    public class ApiTestBase : TestBase, IDisposable
    {
        protected string BaseAddress;
        protected readonly HttpClient Client;
        CancellationTokenSource cancellation;

        public ApiTestBase()
        {
            BaseAddress = "http://webui.papercut.com";
            Client = BuildClient();
        }

        HttpClient BuildClient()
        {
            WebStartup.Scope = this.Scope;

            cancellation = new CancellationTokenSource();
            var testServer = WebStartup.StartInProcessServer(cancellation.Token, "Development");
            return testServer.CreateClient();
        }

        void IDisposable.Dispose()
        {
            Client.Dispose();
            
            foreach(var path in Scope.Resolve<IMessagePathConfigurator>().LoadPaths){
                if(Directory.Exists(path)){
                    Directory.Delete(path, true);
                }
            }

            Scope.Dispose();
            cancellation.Cancel();
        }


        protected HttpResponseMessage Get(string uri)
        {
            return Client.GetAsync(uri).Result;
        }

        protected HttpResponseMessage Delete(string uri)
        {
            return Client.DeleteAsync(uri).Result;
        }

        protected T Get<T>(string uri)
        {
            var response = Get(uri).Content.ReadAsStringAsync().Result;
            return JsonConvert.DeserializeObject<T>(response);
        }
    }
}