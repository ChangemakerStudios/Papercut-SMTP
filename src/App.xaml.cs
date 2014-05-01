namespace Papercut
{
    using System.Windows;

    using Autofac;

    using Papercut.Core;
    using Papercut.UI;

    public partial class App : Application
    {
        ILifetimeScope _lifetimeScope;

        protected override void OnStartup(StartupEventArgs e)
        {
            _lifetimeScope = PapercutContainer.Instance.BeginLifetimeScope();
            base.OnStartup(e);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _lifetimeScope.Dispose();
            base.OnExit(e);
        }

        void Application_Startup(object sender, StartupEventArgs e)
        {
            var mainWindow = _lifetimeScope.Resolve<MainWindow>();
            mainWindow.Show();
        }
    }
}