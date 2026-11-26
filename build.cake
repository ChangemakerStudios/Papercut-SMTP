
#module nuget:?package=Cake.BuildSystems.Module&version=9.0.0

#tool "dotnet:?package=GitVersion.Tool&version=6.4.0"
#tool "dotnet:?package=vpk&version=1.2.158"

#addin "nuget:?package=Cake.Incubator&version=11.0.0"

#nullable enable

#load "./build/Velopack.cake"
#load "./build/Desktop.cake"
#load "./build/WinGet.cake"

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
});

// The web UI's unit tests (Karma + Jasmine), in headless Chrome
Task("TestWeb")
    .Does(() =>
{
    var web = MakeAbsolute(Directory("./src/Papercut.Service/Web"));
    void Npm(string arguments)
    {
        // npm is a .cmd shim on Windows; cmd finds it on the PATH, StartProcess alone does not
        var exitCode = IsRunningOnWindows()
            ? StartProcess("cmd", new ProcessSettings { Arguments = $"/c npm {arguments}", WorkingDirectory = web })
            : StartProcess("npm", new ProcessSettings { Arguments = arguments, WorkingDirectory = web });
        if (exitCode != 0) throw new Exception($"npm {arguments} failed with exit code {exitCode}");
    }

    if (!DirectoryExists(web.Combine("node_modules"))) Npm("ci");

    Npm("test -- --browsers=ChromeHeadless");
});

Task("DeployReleases")
    .WithCriteria(isRunningInGitHubActions && (isMasterBranch || isDevelopBranch) && hasGithubToken)
    .IsDependentOn("BuildAndPackServiceWin64")
    .IsDependentOn("BuildAndPackServiceWin32")
    .IsDependentOn("BuildAndPackServiceWinArm64")
    .IsDependentOn("PrepareWinGetRelease")
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

        // Attach WinGet manifests to the release (master branch only)
        if (isMasterBranch)
        {
            Information($"Attaching WinGet manifests to release {releaseTag}");

            var wingetFiles = GetFiles(releasesDirectory.ToString() + "/winget/*.yaml");
            foreach (var file in wingetFiles)
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
        }
    });

// Windows Service zip per architecture. The task names are what DeployReleases depends on.
foreach (var (serviceTask, serviceRuntime) in new[]
{
    ("BuildAndPackServiceWin64", "win-x64"),
    ("BuildAndPackServiceWin32", "win-x86"),
    ("BuildAndPackServiceWinArm64", "win-arm64")
})
{
    var runtime = serviceRuntime;

    Task(serviceTask)
        .IsDependentOn("Restore")
        .Does(() =>
    {
        CleanDirectory(publishDirectory);

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
    });
}

///////////////////////////////////////////////////////////////////////////////
// WINGET
Task("PrepareWinGetRelease")
    .WithCriteria(isMasterBranch)
    .Does(() =>
{
    var wingetParams = new WinGetReleaseParams
    {
        Version = versionInfo.SemVer,
        ChannelPostfix = channelPostfix,
        ReleasesDirectory = releasesDirectory,
        OutputDirectory = releasesDirectory + Directory("winget")
    };

    WinGet.PrepareRelease(Context, wingetParams);
});

///////////////////////////////////////////////////////////////////////////////
///////////////////////////////////////////////////////////////////////////////
// v8 DESKTOP (Avalonia shell + service/) -- what "All" ships

var desktopPublishDirectory = Directory("./publish/desktop");

foreach (var desktopRuntime in new[] { "win-x64", "win-x86", "win-arm64" })
{
    var runtime = desktopRuntime;

    Task($"PackageDesktop-{runtime}")
        .IsDependentOn("Restore")
        .Does(() =>
    {
        Desktop.Publish(Context, runtime, configuration, desktopPublishDirectory, versionMSBuildSettings);
        Velopack.Pack(Context, Desktop.WindowsPackParams(runtime, channelPostfix, versionInfo.FullSemVer, desktopPublishDirectory, releasesDirectory));

        // Copy installation helper files alongside Setup.exe
        CopyFile("./installation/UI/Install-PapercutSMTP.ps1", releasesDirectory + File("Install-PapercutSMTP.ps1"));
        CopyFile("./installation/README.md", releasesDirectory + File("INSTALLATION.md"));
    });
}

Task("PackageDesktop")
    .IsDependentOn("PackageDesktop-win-x64")
    .IsDependentOn("PackageDesktop-win-x86")
    .IsDependentOn("PackageDesktop-win-arm64");

Task("All")
    .IsDependentOn("Clean")
    .IsDependentOn("Restore")
    .IsDependentOn("Test")
    .IsDependentOn("TestWeb")
    .IsDependentOn("PackageDesktop")
    .IsDependentOn("BuildAndPackServiceWin64")
    .IsDependentOn("BuildAndPackServiceWin32")
    .IsDependentOn("BuildAndPackServiceWinArm64")
    .IsDependentOn("PrepareWinGetRelease")
    .IsDependentOn("DeployReleases");

RunTarget(target);
