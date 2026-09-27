using Avalonia;

namespace Papercut.Shell.Spike;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        using var server = TestServer.Start();

        SpikeLog.Start();
        SpikeLog.Write("spike", "start", $"os={Environment.OSVersion} rid={System.Runtime.InteropServices.RuntimeInformation.RuntimeIdentifier} origin={server.Origin}");

        App.Origin = server.Origin;
        App.EnableDevTools = args.Contains("--devtools");

        AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .LogToTrace()
            .StartWithClassicDesktopLifetime(args, Avalonia.Controls.ShutdownMode.OnExplicitShutdown);

        SpikeLog.Write("spike", "exit", SpikeLog.FilePath);
    }
}
