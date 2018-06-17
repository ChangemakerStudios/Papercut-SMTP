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


namespace Papercut.WebUI.Tests.Base
{
    using System;
    using System.Net.Http;
    using System.Threading;

    using Newtonsoft.Json;

    using Papercut.Service.Infrastructure.WebServer;

    public class ApiTestBase : TestBase, IDisposable
    {
        protected string BaseAddress;
        protected readonly HttpClient Client;
        CancellationTokenSource cancellation;

        public ApiTestBase()
        {
            this.BaseAddress = "http://webui.papercut.com";
            this.Client = this.BuildClient();
        }

        HttpClient BuildClient()
        {
            WebStartup.Scope = this.Scope;

            this.cancellation = new CancellationTokenSource();
            var testServer = WebStartup.StartInProcessServer(this.cancellation.Token, "Development");
            return testServer.CreateClient();
        }

        void IDisposable.Dispose()
        {
            this.Client.Dispose();
            this.Scope.Dispose();
            this.cancellation.Cancel();
        }


        protected HttpResponseMessage Get(string uri)
        {
            return this.Client.GetAsync(uri).Result;
        }

        protected HttpResponseMessage Delete(string uri)
        {
            return this.Client.DeleteAsync(uri).Result;
        }

        protected T Get<T>(string uri)
        {
            var response = this.Get(uri).Content.ReadAsStringAsync().Result;
            return JsonConvert.DeserializeObject<T>(response);
        }
    }
}