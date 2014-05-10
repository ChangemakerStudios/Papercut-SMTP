namespace Papercut.Service
{
    using Papercut.Core;
    using Papercut.Service.Helpers;

    using Topshelf;
    using Topshelf.Autofac;

    class Program
    {
        static void Main(string[] args)
        {
            AssemblyResolutionHelper.SetupEmbeddedAssemblyResolve();

            HostFactory.Run(x =>
            {
                x.UseAutofacContainer(PapercutContainer.Instance);
                x.Service<PapercutService>(s =>
                {
                    s.ConstructUsingAutofacContainer();
                    s.WhenStarted(tc => tc.Start());
                    s.WhenStopped(tc => tc.Stop());
                });

                x.RunAsLocalSystem();

                x.SetDescription("Papercut SMTP Backend Service");
                x.SetDisplayName("Papercut Service");
                x.SetServiceName("PapercutService");
            });
        }
    }
}