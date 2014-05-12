namespace Papercut
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Windows;

    using Autofac;

    using Papercut.Core;
    using Papercut.Core.Events;
    using Papercut.Core.Helper;
    using Papercut.Helpers;

    public partial class App : Application
    {
        Lazy<ILifetimeScope> _lifetimeScope =
            new Lazy<ILifetimeScope>(
                () => PapercutContainer.Instance.BeginLifetimeScope(PapercutContainer.UIScopeTag));

        static App()
        {
            // nothing can be called or loaded before this call is done.
            AssemblyResolutionHelper.SetupEmbeddedAssemblyResolve();
        }

        public ILifetimeScope Container
        {
            get
            {
                return _lifetimeScope.Value;
            }
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            var publishEvent = Container.Resolve<IPublishEvent>();

            var appCanStartEvent = new AppPreStartEvent();
            publishEvent.Publish(appCanStartEvent);

            if (appCanStartEvent.CancelStart)
            {
                // force shut down...
                publishEvent.Publish(new AppForceShutdownEvent());
                return;
            }

            base.OnStartup(e);

            // startup app
            publishEvent.Publish(new AppReadyEvent());
        }

        protected override void OnExit(ExitEventArgs e)
        {
            Debug.WriteLine("App.OnExit()");

            using (Container)
            {
                Container.Resolve<IPublishEvent>().Publish(new AppExitEvent());
            }

            _lifetimeScope = null;
            base.OnExit(e);
        }
    }
}