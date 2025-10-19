
#module nuget:?package=Cake.BuildSystems.Module&version=8.0.0

#tool "nuget:?package=System.Configuration.ConfigurationManager&version=4.5.0"
#tool "nuget:?package=MarkdownSharp&version=2.0.5"
#tool "nuget:?package=MimekitLite&version=4.14.0"
#tool "nuget:?package=NUnit.ConsoleRunner&version=3.17.0"
#tool "nuget:?package=OpenCover&version=4.7.1221"

#tool "dotnet:?package=GitVersion.Tool&version=6.4.0"
#tool "dotnet:?package=vpk&version=0.0.1298"

#addin "nuget:?package=Cake.FileHelpers&version=7.0.0"
#addin "nuget:?package=Cake.Incubator&version=8.0.0"

#nullable enable

#reference "tools/System.Configuration.ConfigurationManager.4.5.0/lib/netstandard2.0/System.Configuration.ConfigurationManager.dll"
#reference "tools/MarkdownSharp.2.0.5/lib/netstandard2.0/MarkdownSharp.dll"
#reference "tools/MimeKitLite.4.14.0/lib/netstandard2.0/MimeKitLite.dll"

#load "./build/ReleaseNotes.cake"
#load "./build/Velopack.cake"

///////////////////////////////////////////////////////////////////////////////
// ARGUMENTS
///////////////////////////////////////////////////////////////////////////////

var configuration = Argument("configuration", "Release");
var target = Argument("target", "All");
GitVersion versionInfo = GitVersion(new GitVersionSettings { OutputType = GitVersionOutput.Json });

var isRunningInGitHubActions = !string.IsNullOrEmpty(EnvironmentVariable("GITHUB_ACTIONS"));
var branchName = EnvironmentVariable("GITHUB_REF_NAME") ?? versionInfo.BranchName;
var isMasterBranch = StringComparer.OrdinalIgnoreCase.Equals("master", branchName);
var isDevelopBranch = StringComparer.OrdinalIgnoreCase.Equals("develop", branchName);
var githubToken = EnvironmentVariable<string?>("GITHUB_TOKEN", null);
var hasGithubToken = !string.IsNullOrEmpty(githubToken);

if (isRunningInGitHubActions)
{
    Information($"Building Branch '{branchName}'...");
}

var channelPostfix = isMasterBranch ? "-stable" : isDevelopBranch ? "-dev" : "-alpha";

///////////////////////////////////////////////////////////////////////////////
// SETUP / TEARDOWN
///////////////////////////////////////////////////////////////////////////////
Setup(ctx =>
{
    Information("Running tasks...");
    Information($"Version: {versionInfo.SemVer}");
});

Teardown(ctx => Information("Finished running tasks."));

///////////////////////////////////////////////////////////////////////////////
// Configuration
///////////////////////////////////////////////////////////////////////////////
var papercutDir = Directory("./src/Papercut.UI");
var papercutServiceDir = Directory("./src/Papercut.Service");
var publishDirectory = Directory("./publish");
var releasesDirectory = Directory("./releases");

// Reusable MSBuild settings with GitVersion properties
var versionMSBuildSettings = new DotNetMSBuildSettings()
    .WithProperty("Version", versionInfo.FullSemVer)
    .WithProperty("AssemblyVersion", versionInfo.AssemblySemVer)
    .WithProperty("FileVersion", versionInfo.AssemblySemFileVer)
    .WithProperty("InformationalVersion", versionInfo.InformationalVersion);

///////////////////////////////////////////////////////////////////////////////
// TASKS
///////////////////////////////////////////////////////////////////////////////
Task("Clean")
    .Does(() =>
{
    var cleanDirectories = new List<string>() { publishDirectory, releasesDirectory };
    foreach (var directory in cleanDirectories)
    {
        CleanDirectory(directory);
    }
});

///////////////////////////////////////////////////////////////////////////////
// RELEASE NOTES
Task("CreateReleaseNotes")
    .Does(() => ReleaseNotes.Create(Context))
    .OnError(exception => Error(exception));

///////////////////////////////////////////////////////////////////////////////
Task("Restore")
    .IsDependentOn("Clean")
    .Does(() =>
{
    DotNetRestore("./Papercut.sln");
});

///////////////////////////////////////////////////////////////////////////////
// TEST
Task("Test")
    .IsDependentOn("Restore")
    .Does(() =>
{
    var testProjects = GetFiles("./test/**/*.Tests.csproj");
    var testResultsDir = Directory("./TestResults");
    EnsureDirectoryExists(testResultsDir);

    foreach (var project in testProjects)
    {
        Information($"Running tests for {project.GetFilename()}");

        var settings = new DotNetTestSettings
        {
            Configuration = configuration,
            NoBuild = false,
            NoRestore = false,
            Verbosity = DotNetVerbosity.Normal,
            Loggers = new[] { $"trx;LogFileName={MakeAbsolute(testResultsDir).FullPath}/{project.GetFilenameWithoutExtension()}.trx" }
        };

        DotNetTest(project.FullPath, settings);
    }
})
.OnError(exception => Error(exception));

///////////////////////////////////////////////////////////////////////////////
// BUILD
Task("BuildUI64")
    .IsDependentOn("Restore")
    .Does(() =>
{
    var settings = new DotNetPublishSettings
    {
        Configuration = configuration,
        Runtime = "win-x64",
        OutputDirectory = publishDirectory,
        EnableCompressionInSingleFile = true,
        MSBuildSettings = versionMSBuildSettings
    };

    DotNetPublish("./src/Papercut.UI/Papercut.csproj", settings);
})
.OnError(exception => Error(exception));

Task("PackageUI64")
    .IsDependentOn("BuildUI64")
    .Does(() =>
{
    var packParams = new VpkPackParams
    {
        Id = "PapercutSMTP",
        Title = "Papercut SMTP",
        Icon = papercutDir + File("App.ico"),
        ReleaseNotes = "ReleaseNotesCurrent.md",
        Channel = "win-x64" + channelPostfix,
        Version = versionInfo.FullSemVer,
        PublishDirectory = publishDirectory,
        ReleaseDirectory = releasesDirectory,
        ExeName = "Papercut.exe",
        Framework = "net8.0-x64-desktop,webview2",
        SplashImage = papercutDir + File("Resources/PapercutSMTP-Installation-Splash.png")
    };

    Velopack.Pack(Context, packParams);

    // Copy installation helper files alongside Setup.exe
    CopyFile("./installation/UI/Install-PapercutSMTP.ps1", releasesDirectory + File("Install-PapercutSMTP.ps1"));
    CopyFile("./installation/README.md", releasesDirectory + File("INSTALLATION.md"));
})
.OnError(exception => Error(exception));

Task("BuildUI32")
    .IsDependentOn("Restore")
    .Does(() =>
{
    CleanDirectory(publishDirectory);

    var settings = new DotNetPublishSettings
    {
        Configuration = configuration,
        Runtime = "win-x86",
        OutputDirectory = publishDirectory,
        EnableCompressionInSingleFile = true,
        MSBuildSettings = versionMSBuildSettings
    };

    DotNetPublish("./src/Papercut.UI/Papercut.csproj", settings);
})
.OnError(exception => Error(exception));

Task("PackageUI32")
    .IsDependentOn("BuildUI32")
    .Does(() =>
{
    // Create a new instance of VpkPackParams with the necessary properties
    var packParams = new VpkPackParams
    {
        Id = "PapercutSMTP",
        Title = "Papercut SMTP",
        Icon = papercutDir + File("App.ico"),
        ReleaseNotes = "ReleaseNotesCurrent.md",
        Channel = "win-x86" + channelPostfix,
        Version = versionInfo.FullSemVer,
        PublishDirectory = publishDirectory,
        ReleaseDirectory = releasesDirectory,
        ExeName = "Papercut.exe",
        Framework = "net8.0-x86-desktop,webview2",
        SplashImage = papercutDir + File("Resources/PapercutSMTP-Installation-Splash.png")
    };

    Velopack.Pack(Context, packParams);

    // Copy installation helper files alongside Setup.exe
    CopyFile("./installation/UI/Install-PapercutSMTP.ps1", releasesDirectory + File("Install-PapercutSMTP.ps1"));
    CopyFile("./installation/README.md", releasesDirectory + File("INSTALLATION.md"));
})
.OnError(exception => Error(exception));

Task("BuildUIArm64")
    .IsDependentOn("Restore")
    .Does(() =>
{
    CleanDirectory(publishDirectory);

    var settings = new DotNetPublishSettings
    {
        Configuration = configuration,
        Runtime = "win-arm64",
        OutputDirectory = publishDirectory,
        EnableCompressionInSingleFile = true,
        MSBuildSettings = versionMSBuildSettings
    };

    DotNetPublish("./src/Papercut.UI/Papercut.csproj", settings);
})
.OnError(exception => Error(exception));

Task("PackageUIArm64")
    .IsDependentOn("BuildUIArm64")
    .Does(() =>
{
    var packParams = new VpkPackParams
    {
        Id = "PapercutSMTP",
        Title = "Papercut SMTP",
        Icon = papercutDir + File("App.ico"),
        ReleaseNotes = "ReleaseNotesCurrent.md",
        Channel = "win-arm64" + channelPostfix,
        Version = versionInfo.FullSemVer,
        PublishDirectory = publishDirectory,
        ReleaseDirectory = releasesDirectory,
        ExeName = "Papercut.exe",
        Framework = "net8.0-arm64-desktop,webview2",
        SplashImage = papercutDir + File("Resources/PapercutSMTP-Installation-Splash.png")
    };

    Velopack.Pack(Context, packParams);

    // Copy installation helper files alongside Setup.exe
    CopyFile("./installation/UI/Install-PapercutSMTP.ps1", releasesDirectory + File("Install-PapercutSMTP.ps1"));
    CopyFile("./installation/README.md", releasesDirectory + File("INSTALLATION.md"));
})
.OnError(exception => Error(exception));

Task("DeployReleases")
    .WithCriteria(isRunningInGitHubActions && (isMasterBranch || isDevelopBranch) && hasGithubToken)
    .IsDependentOn("BuildAndPackServiceWin64")
    .IsDependentOn("BuildAndPackServiceWin32")
    .IsDependentOn("BuildAndPackServiceWinArm64")
    .Does(() =>
    {
        var releaseType = isMasterBranch ? "Release" : "Pre-release";
        var releaseTag = versionInfo.SemVer;

        Information($"Uploading Papercut SMTP 64-bit {releaseType} {versionInfo.FullSemVer}");

        var uploadParams = new VpkUploadParams
        {
            Channel = "win-x64" + channelPostfix,
            ReleaseDirectory = releasesDirectory,
            Token = githubToken ?? "",
            Repository = "https://github.com/ChangemakerStudios/Papercut-SMTP",
            IsPrelease = !isMasterBranch
        };

        Velopack.UploadGithub(Context, uploadParams);

        Information($"Uploading Papercut SMTP 32-bit {releaseType} {versionInfo.FullSemVer}");

        uploadParams = new VpkUploadParams
        {
            Channel = "win-x86" + channelPostfix,
            ReleaseDirectory = releasesDirectory,
            Token = githubToken ?? "",
            Repository = "https://github.com/ChangemakerStudios/Papercut-SMTP",
            IsPrelease = !isMasterBranch
        };

        Velopack.UploadGithub(Context, uploadParams);

        Information($"Uploading Papercut SMTP ARM64 {releaseType} {versionInfo.FullSemVer}");

        uploadParams = new VpkUploadParams
        {
            Channel = "win-arm64" + channelPostfix,
            ReleaseDirectory = releasesDirectory,
            Token = githubToken ?? "",
            Repository = "https://github.com/ChangemakerStudios/Papercut-SMTP",
            IsPrelease = !isMasterBranch
        };

        Velopack.UploadGithub(Context, uploadParams);

        // Attach Service artifacts to the Velopack-created release
        Information($"Attaching Service artifacts to release {releaseTag}");

        var serviceFiles = GetFiles(releasesDirectory.ToString() + "/Papercut.Smtp.Service.*.zip");
        foreach (var file in serviceFiles)
        {
            Information($"Uploading {file.GetFilename()}");
            StartProcess("gh", new ProcessSettings
            {
                Arguments = new ProcessArgumentBuilder()
                    .Append("release").Append("upload")
                    .Append(releaseTag)
                    .AppendQuoted(file.FullPath)
                    .Append("--clobber")
                    .Append("--repo").Append("ChangemakerStudios/Papercut-SMTP"),
                EnvironmentVariables = new Dictionary<string, string>
                {
                    { "GH_TOKEN", githubToken ?? "" }
                }
            });
        }
    })
.OnError(exception => Error(exception));


Task("BuildAndPackServiceWin64")
    .IsDependentOn("Restore")
    .Does(() =>
{
    CleanDirectory(publishDirectory);
    var runtime = "win-x64";

    var settings = new DotNetPublishSettings
    {
        Configuration = configuration,
        OutputDirectory = publishDirectory,
        Runtime = runtime,
        EnableCompressionInSingleFile = true,
        PublishSingleFile = true,
        SelfContained = true,
        MSBuildSettings = versionMSBuildSettings
    };

    DotNetPublish("./src/Papercut.Service/Papercut.Service.csproj", settings);

    CopyFiles("./installation/service/*.ps1", publishDirectory);
    CopyFiles("./installation/service/*.bat", publishDirectory);

    var destFileName = new DirectoryPath(releasesDirectory).CombineWithFilePath($"Papercut.Smtp.Service.{versionInfo.FullSemVer}-{runtime}.zip");
    Zip(publishDirectory, destFileName, GetFiles(publishDirectory.ToString() + "/**/*"));
})
.OnError(exception => Error(exception));

Task("BuildAndPackServiceWin32")
    .IsDependentOn("Restore")
    .Does(() =>
{
    CleanDirectory(publishDirectory);

    var runtime = "win-x86";

    var settings = new DotNetPublishSettings
    {
        Configuration = configuration,
        OutputDirectory = publishDirectory,
        Runtime = runtime,
        EnableCompressionInSingleFile = true,
        PublishSingleFile = true,
        SelfContained = true,
        MSBuildSettings = versionMSBuildSettings
    };

    DotNetPublish("./src/Papercut.Service/Papercut.Service.csproj", settings);

    CopyFiles("./installation/service/*.ps1", publishDirectory);
    CopyFiles("./installation/service/*.bat", publishDirectory);

    var destFileName = new DirectoryPath(releasesDirectory).CombineWithFilePath($"Papercut.Smtp.Service.{versionInfo.FullSemVer}-{runtime}.zip");
    Zip(publishDirectory, destFileName, GetFiles(publishDirectory.ToString() + "/**/*"));
})
.OnError(exception => Error(exception));

Task("BuildAndPackServiceWinArm64")
    .IsDependentOn("Restore")
    .Does(() =>
{
    CleanDirectory(publishDirectory);

    var runtime = "win-arm64";

    var settings = new DotNetPublishSettings
    {
        Configuration = configuration,
        OutputDirectory = publishDirectory,
        Runtime = runtime,
        EnableCompressionInSingleFile = true,
        PublishSingleFile = true,
        SelfContained = true,
        MSBuildSettings = versionMSBuildSettings
    };

    DotNetPublish("./src/Papercut.Service/Papercut.Service.csproj", settings);

    CopyFiles("./installation/service/*.ps1", publishDirectory);
    CopyFiles("./installation/service/*.bat", publishDirectory);

    var destFileName = new DirectoryPath(releasesDirectory).CombineWithFilePath($"Papercut.Smtp.Service.{versionInfo.FullSemVer}-{runtime}.zip");
    Zip(publishDirectory, destFileName, GetFiles(publishDirectory.ToString() + "/**/*"));
})
.OnError(exception => Error(exception));

///////////////////////////////////////////////////////////////////////////////
Task("All")
    .IsDependentOn("Clean")
    .IsDependentOn("CreateReleaseNotes")
    .IsDependentOn("Restore")
    .IsDependentOn("Test")
    .IsDependentOn("BuildUI32").IsDependentOn("PackageUI32")
    .IsDependentOn("BuildUI64").IsDependentOn("PackageUI64")
    .IsDependentOn("BuildUIArm64").IsDependentOn("PackageUIArm64")
    .IsDependentOn("BuildAndPackServiceWin64")
    .IsDependentOn("BuildAndPackServiceWin32")
    .IsDependentOn("BuildAndPackServiceWinArm64")
    .IsDependentOn("DeployReleases")
    .OnError(exception => Error(exception));

RunTarget(target);
