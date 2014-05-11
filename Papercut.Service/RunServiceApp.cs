namespace Papercut.Service
{
    using Autofac;

    using Papercut.Core;
    using Papercut.Service.Logging;

    using Serilog;

    using Topshelf;
    using Topshelf.HostConfigurators;

    class RunServiceApp
    {
        ILifetimeScope _container;

        public void Run()
        {
            _container = PapercutContainer.Instance.BeginLifetimeScope();
            try
            {
                HostFactory.Run(ConfigureHost);
            }
            catch (System.Exception ex)
            {
                _container.Resolve<ILogger>().Fatal(ex, "Unhandled Exception");
                throw;
            }
        }

        void ConfigureHost(HostConfigurator x)
        {
            x.UseSerilog(_container.Resolve<ILogger>());
            x.Service<PapercutService>(s =>
            {
                s.ConstructUsing(serviceFactory => _container.Resolve<PapercutService>());
                s.WhenStarted(tc => tc.Start());
                s.WhenStopped(tc => tc.Stop());
                s.WhenShutdown(ts => _container.Dispose());
            });

            x.RunAsLocalSystem();

            x.SetDescription("Papercut SMTP Backend Service");
            x.SetDisplayName("Papercut SMTP Service");
            x.SetServiceName("PapercutService");
        }
    }
}