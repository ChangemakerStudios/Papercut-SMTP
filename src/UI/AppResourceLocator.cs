namespace Papercut.UI
{
    using System;
    using System.IO;
    using System.Reflection;
    using System.Windows;
    using System.Windows.Resources;

    using Serilog;

    public class AppResourceLocator
    {
        public ILogger Logger { get; set; }

        readonly string _appExecutableName;

        public AppResourceLocator(ILogger logger)
        {
            Logger = logger.ForContext<AppResourceLocator>();
            _appExecutableName = Path.GetFileNameWithoutExtension(Assembly.GetExecutingAssembly().Location);
        }

        public StreamResourceInfo GetResource(string resourceName)
        {
            try
            {
                return Application.GetResourceStream(new Uri(string.Format("/{0};component/{1}", _appExecutableName, resourceName), UriKind.Relative));
            }
            catch (Exception ex)
            {
                Logger.Error(
                    ex,
                    "Failure loading application resource {ResourceName} in {ExecutableName}",
                    resourceName,
                    _appExecutableName);

                throw;
            }
        }
    }
}