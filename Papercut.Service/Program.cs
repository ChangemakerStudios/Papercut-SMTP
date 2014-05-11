namespace Papercut.Service
{
    using System.Runtime.InteropServices;

    using Papercut.Service.Helpers;

    class Program
    {
        static RunServiceApp app = null;

        static void Main(string[] args)
        {
            AssemblyResolutionHelper.SetupEmbeddedAssemblyResolve();
            app = new RunServiceApp();
            app.Run();
        }
    }
}