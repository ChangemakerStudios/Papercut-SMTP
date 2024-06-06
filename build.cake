
#tool "nuget:?package=System.Configuration.ConfigurationManager&version=4.5.0"
#tool "nuget:?package=MarkdownSharp&version=2.0.5"
#tool "nuget:?package=MimekitLite&version=4.5.0"
#tool "nuget:?package=NUnit.ConsoleRunner&version=3.17.0"
#tool "nuget:?package=OpenCover&version=4.7.1221"

#tool "dotnet:?package=GitVersion.Tool&version=5.12.0"
#tool "dotnet:?package=vpk&version=0.0.359"

#addin "nuget:?package=Cake.FileHelpers&version=6.1.3"
#addin "nuget:?package=Cake.Incubator&version=8.0.0"

#nullable enable

#reference "tools/System.Configuration.ConfigurationManager.4.5.0/lib/netstandard2.0/System.Configuration.ConfigurationManager.dll"
#reference "tools/MarkdownSharp.2.0.5/lib/netstandard2.0/MarkdownSharp.dll"
#reference "tools/MimeKitLite.4.5.0/lib/netstandard2.0/MimeKitLite.dll"

#load "./build/ReleaseNotes.cake"
#load "./build/Velopack.cake"

///////////////////////////////////////////////////////////////////////////////
// ARGUMENTS
///////////////////////////////////////////////////////////////////////////////

var configuration = Argument("configuration", "Release");
var target = Argument("target", "All");
GitVersion versionInfo = GitVersion(new GitVersionSettings { OutputType = GitVersionOutput.Json });

var isMasterBranch = false;
var isDevelopBranch = false;
var hasGithubToken = false;
string? githubToken = null;

if (AppVeyor.IsRunningOnAppVeyor)
{
    Information($"Building Branch '{BuildSystem.AppVeyor.Environment.Repository.Branch}'...");
    isMasterBranch = StringComparer.OrdinalIgnoreCase.Equals("master", BuildSystem.AppVeyor.Environment.Repository.Branch);
    isDevelopBranch = StringComparer.OrdinalIgnoreCase.Equals("develop", BuildSystem.AppVeyor.Environment.Repository.Branch);
    githubToken = EnvironmentVariable<string?>("github-token", null);
}

if (!string.IsNullOrEmpty(githubToken))
{
    hasGithubToken = true;
}

var channelPostfix = isMasterBranch ? "-stable" : isDevelopBranch ? "-dev" : "-alpha";

///////////////////////////////////////////////////////////////////////////////
// SETUP / TEARDOWN
///////////////////////////////////////////////////////////////////////////////
Setup(ctx =>
{
    Information("Running tasks...");

    if (AppVeyor.IsRunningOnAppVeyor)
    {
        AppVeyor.UpdateBuildVersion(versionInfo.SemVer);
    }
});

Teardown(ctx => Information("Finished running tasks."));

///////////////////////////////////////////////////////////////////////////////
// Configuration
///////////////////////////////////////////////////////////////////////////////
var papercutDir = Directory("./src/Papercut.UI");
var papercutServiceDir = Directory("./src/Papercut.Service");
var publishDirectory = Directory("./publish");
var releasesDirectory = Directory("./releases");

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
// Assembly Version Patching
Task("PatchAssemblyInfo")
    .Does(() =>
{
    GitVersion(new GitVersionSettings
    {
        UpdateAssemblyInfo = true,
        OutputType = GitVersionOutput.BuildServer,
        UpdateAssemblyInfoFilePath = "./src/GlobalAssemblyInfo.cs"
    });
})
.OnError(exception => Error(exception));

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

Task("DownloadReleases")
    .WithCriteria(hasGithubToken)
    .IsDependentOn("Restore")
    .Does(() =>
{
    var arguments = new ProcessArgumentBuilder()
        .Append("download").Append("github")
        .Append("--repoUrl").Append("https://github.com/ChangemakerStudios/Papercut-SMTP")
        .Append("--token").Append(EnvironmentVariable<string>("github-token", ""));

    StartProcess("vpk", new ProcessSettings
    {
        Arguments = arguments
    });
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
        EnableCompressionInSingleFile = true
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
        Framework = "net8.0-x64-desktop,webview2"
    };

    Velopack.Pack(Context, packParams);
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
        EnableCompressionInSingleFile = true
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
        Framework = "net8.0-x86-desktop,webview2"
    };

    Velopack.Pack(Context, packParams);
})
.OnError(exception => Error(exception));

Task("UploadArtifacts")
    .WithCriteria(AppVeyor.IsRunningOnAppVeyor)
    .IsDependentOn("PackageUI32")
    .Does(() =>
    {
        foreach (var file in GetFiles(releasesDirectory.ToString() + "/**/*.zip"))
        {
            Information($"Uploading Artifact to AppVeyor: {file}");
            AppVeyor.UploadArtifact(file);
        }
    })
.OnError(exception => Error(exception));

Task("DeployReleases")
    .WithCriteria((isMasterBranch || isDevelopBranch) && hasGithubToken)
    .IsDependentOn("UploadArtifacts")
    .Does(() =>
    {
        Information($"Uploading Papercut SMTP 64-bit Release {GitVersionOutput.BuildServer}");

        var uploadParams = new VpkUploadParams
        {
            Channel = "win-x64" + channelPostfix,
            ReleaseDirectory = releasesDirectory,
            Token = EnvironmentVariable<string>("github-token", ""),
            Repository = "https://github.com/ChangemakerStudios/Papercut-SMTP",
            IsPrelease = !isMasterBranch
        };

        Velopack.UploadGithub(Context, uploadParams);

        Information($"Uploading Papercut SMTP 32-bit Release {GitVersionOutput.BuildServer}");

        uploadParams = new VpkUploadParams
        {
            Channel = "win-x86" + channelPostfix,
            ReleaseDirectory = releasesDirectory,
            Token = EnvironmentVariable<string>("github-token", ""),
            Repository = "https://github.com/ChangemakerStudios/Papercut-SMTP",
            IsPrelease = !isMasterBranch
        };

        Velopack.UploadGithub(Context, uploadParams);
    })
.OnError(exception => Error(exception));


Task("BuildAndPackServiceWin64")
    .IsDependentOn("Restore")
    .Does(() =>
{
    CleanDirectory(publishDirectory);
    CleanDirectory(releasesDirectory);

    var runtime = "win-x64";

    var settings = new DotNetPublishSettings
    {
        Configuration = configuration,
        OutputDirectory = publishDirectory,
        Runtime = runtime,
        EnableCompressionInSingleFile = true,
        PublishSingleFile = true,
        SelfContained = true,
    };

    DotNetPublish("./src/Papercut.Service/Papercut.Service.csproj", settings);

    CopyFiles("./extra/*.ps1", publishDirectory);

    var destFileName = new DirectoryPath(releasesDirectory).CombineWithFilePath($"Papercut.Smtp.Service.{versionInfo.FullSemVer}-{runtime}.zip");
    Zip(publishDirectory, destFileName, GetFiles(publishDirectory.ToString() + "/**/*"));
})
.OnError(exception => Error(exception));

Task("BuildAndPackServiceWin32")
    .IsDependentOn("Restore")
    .Does(() =>
{
    CleanDirectory(publishDirectory);
    CleanDirectory(releasesDirectory);

    var runtime = "win-x86";

    var settings = new DotNetPublishSettings
    {
        Configuration = configuration,
        OutputDirectory = publishDirectory,
        Runtime = runtime,
        EnableCompressionInSingleFile = true,
        PublishSingleFile = true,
        SelfContained = true,
    };

    DotNetPublish("./src/Papercut.Service/Papercut.Service.csproj", settings);

    CopyFiles("./extra/*.ps1", publishDirectory);

    var destFileName = new DirectoryPath(releasesDirectory).CombineWithFilePath($"Papercut.Smtp.Service.{versionInfo.FullSemVer}-{runtime}.zip");
    Zip(publishDirectory, destFileName, GetFiles(publishDirectory.ToString() + "/**/*"));
})
.OnError(exception => Error(exception));

///////////////////////////////////////////////////////////////////////////////
Task("All")
    .IsDependentOn("Clean")
    .IsDependentOn("PatchAssemblyInfo")
    .IsDependentOn("CreateReleaseNotes")
    .IsDependentOn("DownloadReleases")
    .IsDependentOn("Restore")
    .IsDependentOn("BuildUI32").IsDependentOn("PackageUI32")
    .IsDependentOn("BuildUI64").IsDependentOn("PackageUI64")
    .IsDependentOn("DeployReleases")
    .IsDependentOn("BuildAndPackServiceWin32")
    .IsDependentOn("BuildAndPackServiceWin64")
    .IsDependentOn("UploadArtifacts")
    .OnError(exception => Error(exception));

RunTarget(target);
