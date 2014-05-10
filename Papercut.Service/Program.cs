namespace Papercut.Service
{
    using Autofac;

    using Papercut.Core;
    using Papercut.Service.Helpers;
    using Papercut.Service.Logging;

    using Serilog;

    using Topshelf;
    using Topshelf.Autofac;

    class Program
    {
        static void Main(string[] args)
        {
            AssemblyResolutionHelper.SetupEmbeddedAssemblyResolve();

            HostFactory.Run(
                x =>
                {
                    var lifetime = PapercutContainer.Instance.BeginLifetimeScope();

                    x.UseSerilog(lifetime.Resolve<ILogger>());
                    x.UseAutofacContainer(lifetime);
                    x.Service<PapercutService>(
                        s =>
                        {
                            s.ConstructUsingAutofacContainer();
                            s.WhenStarted(tc => tc.Start());
                            s.WhenStopped(tc => tc.Stop());
                            s.WhenShutdown(ts => lifetime.Dispose());
                        });

                    x.RunAsLocalSystem();

                    x.SetDescription("Papercut SMTP Backend Service");
                    x.SetDisplayName("Papercut Service");
                    x.SetServiceName("PapercutService");
                });
        }
    }
}