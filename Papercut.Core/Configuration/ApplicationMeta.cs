namespace Papercut.Core.Configuration
{
    using System.Reflection;

    public class ApplicationMeta : IAppMeta
    {
        public ApplicationMeta(string appName, string appVersion = null)
        {
            AppName = appName;
            AppVersion = appVersion ?? Assembly.GetCallingAssembly().GetName().Version.ToString(3);
        }

        public string AppName { get; private set; }
        public string AppVersion { get; private set; }
    }
}