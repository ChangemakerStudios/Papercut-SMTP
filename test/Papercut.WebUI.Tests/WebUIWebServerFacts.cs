namespace Papercut.WebUI.Tests
{
    using System;
    using System.Net;

    using Autofac;

    using Common.Domain;

    using Core.Domain.Application;
    using Core.Infrastructure.Container;
    using Core.Infrastructure.Lifecycle;

    using Xunit;

    public class WebUIWebServerFacts : IDisposable
    {
        readonly ILifetimeScope _scope;

        public WebUIWebServerFacts()
        {
            _scope = BuildContainer(MockDependencies).BeginLifetimeScope();
        }

        [Fact]
        void should_bootstrap_http_server_and_serve_health_check()
        {
            _scope.Resolve<IMessageBus>().Publish(new PapercutServiceReadyEvent { AppMeta = _scope.Resolve<IAppMeta>() });
            
            var content = new WebClient().DownloadString("http://localhost:6789/health");

            Assert.Equal("Papercut WebUI Server Start Success", content);
        }

        void IDisposable.Dispose()
        {
            _scope.Dispose();
        }
        
        static IContainer BuildContainer(Action<ContainerBuilder> configurer = null)
        {
            PapercutContainer.SpecifiedEntryAssembly = typeof(WebUIWebServerFacts).Assembly;

            var builder = new ContainerBuilder();

            builder.RegisterModule<PapercutCoreModule>();
            configurer?.Invoke(builder);

            return builder.Build();
        }

        public virtual void MockDependencies(ContainerBuilder builder)
        {
            builder.Register(c => new ApplicationMeta("Papercut.WebUI.Tests"))
                .As<IAppMeta>()
                .SingleInstance();
        }
    }
}
