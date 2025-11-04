// Papercut
// 
// Copyright © 2008 - 2012 Ken Robertson
// Copyright © 2013 - 2025 Jaben Cargman
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


using Microsoft.AspNetCore.Hosting.Server;

namespace Papercut.Service.Web.Hosting.InProcess;

public class HttpServer : IServer
{
    private bool _disposed;

    public HttpServer(IWebHostBuilder builder)
    {
        var host = builder.UseServer(this).Build();
        host.Start();
        Host = host;
    }

    public Uri BaseAddress { get; set; } = new("http://localhost/");

    public IWebHost Host { get; }

    IFeatureCollection IServer.Features { get; } = new FeatureCollection();

    //public HttpMessageHandler CreateHandler()
    //{
    //    var pathBase = BaseAddress == null ? PathString.Empty : PathString.FromUriComponent(BaseAddress);
    //    return new ClientHandler(pathBase, _application);
    //}

    //public HttpClient CreateClient()
    //{
    //    return new HttpClient(CreateHandler()) { BaseAddress = BaseAddress };
    //}

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            Host.Dispose();
        }
    }

    Task IServer.StartAsync<TContext>(IHttpApplication<TContext> application, CancellationToken cancellationToken)
    {
        //_application = new ApplicationWrapper<Context>((IHttpApplication<Context>)application, () =>
        //{
        //    if (_disposed)
        //    {
        //        throw new ObjectDisposedException(GetType().FullName);
        //    }
        //});
        return Task.FromResult(0);
    }

    Task IServer.StopAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult(0);
    }

    //private class ApplicationWrapper<TContext> : IHttpApplication<TContext>
    //{
    //    private readonly IHttpApplication<TContext> _application;
    //    private readonly Action _preProcessRequestAsync;

    //    public ApplicationWrapper(IHttpApplication<TContext> application, Action preProcessRequestAsync)
    //    {
    //        _application = application;
    //        _preProcessRequestAsync = preProcessRequestAsync;
    //    }

    //    public TContext CreateContext(IFeatureCollection contextFeatures)
    //    {
    //        return _application.CreateContext(contextFeatures);
    //    }

    //    public void DisposeContext(TContext context, Exception exception)
    //    {
    //        _application.DisposeContext(context, exception);
    //    }

    //    public Task ProcessRequestAsync(TContext context)
    //    {
    //        _preProcessRequestAsync();
    //        return _application.ProcessRequestAsync(context);
    //    }
    //}
}