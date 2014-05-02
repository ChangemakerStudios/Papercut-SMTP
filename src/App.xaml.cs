namespace Papercut
{
    using System;
    using System.Diagnostics;
    using System.Linq;
    using System.Reflection;
    using System.Windows;

    using Autofac;

    using Papercut.Core;
    using Papercut.UI;

    public partial class App : Application
    {
        ILifetimeScope _lifetimeScope;

        public App()
        {
            SetupEmbeddedAssemblyResolve();
        }

        static void SetupEmbeddedAssemblyResolve()
        {
            Assembly thisAssembly = Assembly.GetExecutingAssembly();
            string[] validExtensions = { ".dll", ".exe" };
            string[] resourceNames = thisAssembly.GetManifestResourceNames();

            // Code based on: http://www.codingmurmur.com/2014/02/embedded-assembly-loading-with-support.html
            AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
            {
                var name = args.Name;
                var asmName = new AssemblyName(name);

                // Any retargetable assembly should be resolved directly using normal load e.g. System.Core issue: 
                // http://stackoverflow.com/questions/18793959/filenotfoundexception-when-trying-to-load-autofac-as-an-embedded-assembly
                if (name.EndsWith("Retargetable=Yes")) return Assembly.Load(asmName);

                var possibleResourceNames = validExtensions.Select(ext => string.Format("{0}{1}", asmName.Name, ext)).ToList();
                var resourceToFind = string.Join(",", possibleResourceNames);
                var resourceName = resourceNames.SingleOrDefault(n => possibleResourceNames.Any(n.Contains));

                if (string.IsNullOrWhiteSpace(resourceName)) return null;

                var symbolsToFind = asmName.Name + ".pdb";
                var symbolsName = resourceNames.SingleOrDefault(n => n.Contains(symbolsToFind));

                var assemblyData = LoadResourceBytes(thisAssembly, resourceName);

                if (string.IsNullOrWhiteSpace(symbolsName))
                {
                    Trace.WriteLine(string.Format("Loading '{0}' as embedded resource '{1}'", resourceToFind, resourceName));

                    return Assembly.Load(assemblyData);
                }

                var symbolsData = LoadResourceBytes(thisAssembly, symbolsName);

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
            using (var stream = executingAssembly.GetManifestResourceStream(resourceName))
            {
                var assemblyData = new byte[stream.Length];
                stream.Read(assemblyData, 0, assemblyData.Length);
                return assemblyData;
            }
        }

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