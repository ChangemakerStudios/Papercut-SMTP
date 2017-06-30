

namespace Papercut.WebUI
{
    using Autofac;
    using Autofac.Core;

    using Core.Infrastructure.Lifecycle;
    using Core.Infrastructure.Plugins;
    using Common.Domain;

    public class WebUIPluginModule: Module, IPluginModule
    {
        public IModule Module => this;
        public string Name => "WebUI";
        public string Version => "1.0.0";
        public string Description => "Provides a web UI to manage the email messages for Papercut.";

        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<WebServer>().As<IEventHandler<PapercutServiceReadyEvent>>().SingleInstance();
            base.Load(builder);
        }
    }


    public class WebServer : IEventHandler<PapercutServiceReadyEvent>
    {
        public void Handle(PapercutServiceReadyEvent @event)
        {
            // 
        }
    }
}
