#tool "nuget:?package=MarkdownSharp&version=2.0.5"
#tool "nuget:?package=MimekitLite&version=4.5.0"
#tool "nuget:?package=NUnit.ConsoleRunner&version=3.17.0"
#tool "nuget:?package=OpenCover&version=4.7.1221"
#tool "nuget:?package=GitVersion.CommandLine&version=5.12.0"
#tool "nuget:?package=vpk&version=0.0.359"

#addin "nuget:?package=Cake.FileHelpers&version=6.1.3"
#addin "nuget:?package=Cake.Incubator&version=8.0.0"

#reference "tools/MarkdownSharp.2.0.5/lib/net40/MarkdownSharp.dll"
#reference "tools/MimeKitLite.4.5.0/lib/netstandard2.0/MimeKitLite.dll"

#load "./build/BuildInformation.cake"
#load "./build/ReleaseNotes.cake"

///////////////////////////////////////////////////////////////////////////////
// ARGUMENTS
///////////////////////////////////////////////////////////////////////////////

var target = Argument("target", "All");
var configuration = Argument("configuration", "Release");
//var buildInformation = BuildInformation.GetBuildInformation(Context, BuildSystem);
GitVersion versionInfo = GitVersion(new GitVersionSettings { OutputType = GitVersionOutput.Json });

var isMasterBranch = StringComparer.OrdinalIgnoreCase.Equals("master", BuildSystem.AppVeyor.Environment.Repository.Branch));

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
Task("Restore")
    .IsDependentOn("Clean")
    .Does(() =>
{
    DotNetRestore("./Papercut.sln");
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
// BUILD
Task("BuildUI64")
    .IsDependentOn("Restore")
    .Does(() =>
{
    var settings = new DotNetPublishSettings
    {
        Configuration = configuration,
        Runtime = "win-x64",
        OutputDirectory = publishDirectory
    };

    DotNetPublish("./src/Papercut.UI/Papercut.csproj", settings);
})
.OnError(exception => Error(exception));

Task("BuildUI32")
    .IsDependentOn("Restore")
    .Does(() =>
{
    var settings = new DotNetPublishSettings
    {
        Configuration = configuration,
        Runtime = "win-x86",
        OutputDirectory = publishDirectory
    };

    DotNetPublish("./src/Papercut.UI/Papercut.csproj", settings);
})
.OnError(exception => Error(exception));

///////////////////////////////////////////////////////////////////////////////
// TESTS
// Task("Test")
//     .IsDependentOn("Build64")
//     .Does(() =>
// {
//     NUnit3("./test/**/bin/x64/" + configuration + "/*.Test.dll", new NUnit3Settings()); // { NoResults = true });

//     if (AppVeyor.IsRunningOnAppVeyor)
//     {
//         AppVeyor.UploadTestResults("TestResult.xml", AppVeyorTestResultsType.NUnit3);
//     }
// })
// .OnError(exception => Error(exception));

///////////////////////////////////////////////////////////////////////////////
// PACKAGE STEPS

void MaybeUploadArtifact(FilePath fileName)
{
    if (AppVeyor.IsRunningOnAppVeyor)
    {
        Information("Uploading Artifact to AppVeyor: " + fileName);
        AppVeyor.UploadArtifact(fileName);
    }
}

Task("PackageUI64")
    .IsDependentOn("BuildUI64")
    .Does(() =>
{
    var arguments = new ProcessArgumentBuilder()
            .Append("pack")
            .Append("-u").Append("PapercutSMTP")
            .Append("--packTitle").AppendQuoted("Papercut SMTP")
            .Append("--runtime").Append("win7-x64")
            .Append("--icon").AppendQuoted(papercutDir + File("App.ico"))
            .Append("-v").AppendQuoted(versionInfo.FullSemVer)
            .Append("-p").AppendQuoted(publishDirectory)
            .Append("-o").AppendQuoted(releasesDirectory)
            .Append("-e").AppendQuoted("Papercut.exe")
            .Append("--framework").AppendQuoted("net8.0-x64-desktop,webview2");

    Information("Running Velopack with arguments: " + arguments.Render());
    StartProcess("vpk", new ProcessSettings
    {
        Arguments = arguments
    });

    foreach (var file in GetFiles(releasesDirectory.ToString() + "/**/*"))
    {
        MaybeUploadArtifact(file);
    }
})
.OnError(exception => Error(exception));

Task("DeployUI64")
    .WithCriteria(isMasterBranch)
    .IsDependentOn("PackageUI64")
    .Does(() =>
{
    Information($"Deploying Papercut SMTP Desktop Release {GitVersionOutput.BuildServer}");

    var arguments = new ProcessArgumentBuilder()
        .Append("upload").Append("github")
        .Append("--repoUrl").Append("https://github.com/ChangemakerStudios/Papercut-SMTP");
            .Append("--token").Append(EnvironmentVariable<string>("github-token", ''));

    StartProcess("vpk", new ProcessSettings
    {
        Arguments = arguments
    });
})
.OnError(exception => Error(exception));

// var appFileName = outputDirectory.CombineWithFilePath(string.Format("Papercut.Smtp.x64.{0}.zip", versionInfo.FullSemVer));
// Zip(appBuildDir64, appFileName, GetFiles(appBuildDir64.ToString() + "/**/*"));
// MaybeUploadArtifact(appFileName);

// var chocolateyFileName = outputDirectory.CombineWithFilePath(string.Format("papercut.{0}.nupkg", versionInfo.NuGetVersion));
// ChocolateyPack(
//     File("./chocolatey/Papercut.nuspec"),
//     new ChocolateyPackSettings
//     {
//         Version = versionInfo.NuGetVersion,
//         OutputDirectory = outputDirectory
//     });

// Task("PackagePapercut32")
//     .Does(() =>
// {
//     // remove the apppublish directory
//     var publishDir = appBuildDir32 + Directory("./app.publish");
//     DeleteDirectory(publishDir, new DeleteDirectorySettings { Recursive = true, Force = true });

//     var appFileName = outputDirectory.CombineWithFilePath(string.Format("Papercut.Smtp.x86.{0}.zip", versionInfo.FullSemVer));
//     Zip(appBuildDir32, appFileName, GetFiles(appBuildDir32.ToString() + "/**/*"));
//     MaybeUploadArtifact(appFileName);
// })
// .OnError(exception => Error(exception));

// Task("PackagePapercutService")
//     .Does(() =>
// {
//     var svcFileName = outputDirectory.CombineWithFilePath(string.Format("Papercut.Smtp.Service.{0}.zip", versionInfo.FullSemVer));
//     Zip(svcBuildDir, svcFileName, GetFiles(svcBuildDir.ToString() + "/**/*"));
//     MaybeUploadArtifact(svcFileName);

// })
// .OnError(exception => Error(exception));

// Task("PackageSetup")
//     .Does(() =>
// {
//     MaybeUploadArtifact("./src/Papercut.Bootstrapper/bin/x64/" + configuration + "/Papercut.Smtp.Setup.exe");
//     MaybeUploadArtifact("./src/Papercut.Bootstrapper/bin/x86/" + configuration + "/Papercut.Smtp.Setup.exe");
// })
// .OnError(exception => Error(exception));

///////////////////////////////////////////////////////////////////////////////
Task("All")
    .IsDependentOn("PatchAssemblyInfo")
    .IsDependentOn("Clean")
    //.IsDependentOn("CreateReleaseNotes")
    .IsDependentOn("Restore")
    .IsDependentOn("BuildUI64")
    .IsDependentOn("PackageUI64")
    .IsDependentOn("DeployUI64")
    // .IsDependentOn("PackagePapercut32")
    // .IsDependentOn("PackagePapercutService")
    // .IsDependentOn("PackageSetup")
    .OnError(exception => Error(exception));

RunTarget(target);
