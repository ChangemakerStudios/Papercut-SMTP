

namespace Papercut.WebUI
{
    using Autofac.Core;
    using Core.Infrastructure.Plugins;

    public class WebUIPluginModule: IPluginModule, IModule
    {
        public IModule Module => this;
        public string Name => "WebUI";
        public string Version => "1.0.0";
        public string Description => "Provides a web UI to manage the email messages for Papercut.";




        public void Configure(IComponentRegistry componentRegistry)
        {
            
        }
    }
}
