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

namespace Papercut.App.WebApi.Tests.Base
{
    using System;
    using System.Net.Http;
    using System.Web.Http;

    using Autofac;

    using Papercut.App.WebApi;
    using Papercut.App.WebApi.Tests.WebServerFacts;
    using Papercut.Core.Infrastructure.Container;

    public class ApiTestBase : IDisposable
    {
        protected readonly HttpClient _client;

        protected string _baseAddress;

        protected IContainer _container;

        public ApiTestBase()
        {
            this._baseAddress = "http://webui.papercut.com";
            this._container = new SimpleContainer<ApiTestModule>().Build();
            this._client = this.BuildClient();
        }

        void IDisposable.Dispose()
        {
            this._client.Dispose();
            this._container.Dispose();
        }

        HttpClient BuildClient()
        {
            var config = new HttpConfiguration();

            RouteConfig.Init(config, this._container);

            return new HttpClient(new HttpServer(config))
            {
                BaseAddress = new Uri(this._baseAddress)
            };
        }

        protected HttpResponseMessage Get(string uri)
        {
            return this._client.GetAsync(uri).Result;
        }

        protected HttpResponseMessage Delete(string uri)
        {
            return this._client.DeleteAsync(uri).Result;
        }

        protected T Get<T>(string uri)
        {
            return this.Get(uri).Content.ReadAsAsync<T>().Result;
        }
    }
}