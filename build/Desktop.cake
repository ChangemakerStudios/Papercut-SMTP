// v8 desktop package: the Avalonia shell (Papercut.exe) with Papercut.Service in service/.
// Same Velopack id, exe name and AUMID as the v7 WPF app, so v7 installs update to it in place.
// See docs/plans/v8-desktop.md (Packaging).

public static class Desktop
{
    public const string PackId = "PapercutSMTP";

    public const string Aumid = "ChangemakerStudios.PapercutSMTP";

    public const string WindowsTfm = "net10.0-windows10.0.19041.0";

    /// <summary>
    /// Publishes the shell to publishDir and the service to publishDir/service, both self-contained.
    /// </summary>
    public static void Publish(ICakeContext context, string runtime, string configuration, DirectoryPath publishDir, DotNetMSBuildSettings baseMsbuild)
    {
        context.CleanDirectory(publishDir);

        // no .pdb files in the package
        var msbuild = new DotNetMSBuildSettings();
        foreach (var property in baseMsbuild.Properties) msbuild.Properties[property.Key] = property.Value.ToList();
        msbuild.WithProperty("DebugType", "none").WithProperty("DebugSymbols", "false");

        context.DotNetPublish("./src/Papercut.Desktop/Papercut.Desktop.csproj", new DotNetPublishSettings
        {
            Configuration = configuration,
            Framework = runtime.StartsWith("win-") ? WindowsTfm : "net10.0",
            Runtime = runtime,
            SelfContained = true,
            OutputDirectory = publishDir,
            MSBuildSettings = msbuild
        });

        // the service keeps its own runtime: it needs ASP.NET Core, the shell does not
        context.DotNetPublish("./src/Papercut.Service/Papercut.Service.csproj", new DotNetPublishSettings
        {
            Configuration = configuration,
            Runtime = runtime,
            SelfContained = true,
            PublishSingleFile = true,
            EnableCompressionInSingleFile = true,
            OutputDirectory = publishDir.Combine("service"),
            MSBuildSettings = msbuild
        });

        // the shell runs the service as Production; a Development file would only confuse
        var devSettings = publishDir.Combine("service").CombineWithFilePath("appsettings.Development.json");
        if (context.FileExists(devSettings)) context.DeleteFile(devSettings);
    }

    public static VpkPackParams WindowsPackParams(string runtime, string channelPostfix, string version, DirectoryPath publishDir, DirectoryPath releasesDir) =>
        new VpkPackParams
        {
            Id = PackId,
            Title = "Papercut SMTP",
            Icon = "./src/Papercut.Desktop/Assets/papercut.ico",
            ReleaseNotes = "ReleaseNotesCurrent.md",
            Channel = runtime + channelPostfix,
            Version = version,
            PublishDirectory = publishDir.FullPath,
            ReleaseDirectory = releasesDir.FullPath,
            ExeName = "Papercut.exe",
            // self-contained: only the WebView2 runtime is a prerequisite
            Framework = "webview2",
            SplashImage = "./src/Papercut.Desktop/packaging/PapercutSMTP-Installation-Splash.png",
            Aumid = Aumid
        };
}
