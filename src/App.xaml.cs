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

    public partial class App : Application
    {
        Lazy<ILifetimeScope> _lifetimeScope =
            new Lazy<ILifetimeScope>(
                () => PapercutContainer.Instance.BeginLifetimeScope(PapercutContainer.UIScopeTag));

        static App()
        {
            // nothing can be called or loaded before this call is done.
            SetupEmbeddedAssemblyResolve();
        }

        public ILifetimeScope Container
        {
            get
            {
                return _lifetimeScope.Value;
            }
        }

        static void SetupEmbeddedAssemblyResolve()
        {
            Assembly thisAssembly = Assembly.GetExecutingAssembly();
            string[] validExtensions = { ".dll", ".exe" };
            string[] resourceNames = thisAssembly.GetManifestResourceNames();

            // Code based on: http://www.codingmurmur.com/2014/02/embedded-assembly-loading-with-support.html
            AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
            {
                string name = args.Name;
                var asmName = new AssemblyName(name);

                // Any retargetable assembly should be resolved directly using normal load e.g. System.Core issue: 
                // http://stackoverflow.com/questions/18793959/filenotfoundexception-when-trying-to-load-autofac-as-an-embedded-assembly
                if (name.EndsWith("Retargetable=Yes")) return Assembly.Load(asmName);

                List<string> possibleResourceNames =
                    validExtensions.Select(ext => string.Format("{0}{1}", asmName.Name, ext))
                        .ToList();
                string resourceToFind = string.Join(",", possibleResourceNames);
                string resourceName =
                    resourceNames.SingleOrDefault(n => possibleResourceNames.Any(n.Contains));

                if (string.IsNullOrWhiteSpace(resourceName)) return null;

                string symbolsToFind = asmName.Name + ".pdb";
                string symbolsName = resourceNames.SingleOrDefault(n => n.Contains(symbolsToFind));

                byte[] assemblyData = LoadResourceBytes(thisAssembly, resourceName);

                if (string.IsNullOrWhiteSpace(symbolsName))
                {
                    Trace.WriteLine(
                        string.Format(
                            "Loading '{0}' as embedded resource '{1}'",
                            resourceToFind,
                            resourceName));

                    return Assembly.Load(assemblyData);
                }

                byte[] symbolsData = LoadResourceBytes(thisAssembly, symbolsName);

                Trace.WriteLine(
                    string.Format(
                        "Loading '{0}' as embedded resource '{1}' with symbols '{2}'",
                        resourceToFind,
                        resourceName,
                        symbolsName));

                return Assembly.Load(assemblyData, symbolsData);
            };
        }

        static byte[] LoadResourceBytes(Assembly executingAssembly, string resourceName)
        {
            using (Stream stream = executingAssembly.GetManifestResourceStream(resourceName))
            {
                var assemblyData = new byte[stream.Length];
                stream.Read(assemblyData, 0, assemblyData.Length);
                return assemblyData;
            }
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            Container.Resolve<IPublishEvent>().Publish(new AppReadyEvent());
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