#tool "nuget:?package=MarkdownSharp&version=1.13.0"
#tool "nuget:?package=MimekitLite&version=2.0.6"
#tool "nuget:?package=NUnit.ConsoleRunner&version=3.9.0"
#tool "nuget:?package=OpenCover&version=4.6.519"
#tool "nuget:?package=GitVersion.CommandLine&version=5.2.4"

#addin "nuget:?package=Cake.FileHelpers&version=3.2.1"
#addin "nuget:?package=Cake.Incubator&version=5.1.0"

#reference "tools/MarkdownSharp.1.13.0.0/lib/35/MarkdownSharp.dll"
#reference "tools/MimeKitLite.2.0.6/lib/net45/MimeKitLite.dll"

#load "./BuildInformation.cake"
#load "./ReleaseNotes.cake"

///////////////////////////////////////////////////////////////////////////////
// ARGUMENTS
///////////////////////////////////////////////////////////////////////////////

var target = Argument("target", "All");
var configuration = Argument("configuration", "Release");
//var buildInformation = BuildInformation.GetBuildInformation(Context, BuildSystem);
GitVersion versionInfo = GitVersion(new GitVersionSettings{ OutputType = GitVersionOutput.Json });

///////////////////////////////////////////////////////////////////////////////
// SETUP / TEARDOWN
///////////////////////////////////////////////////////////////////////////////
Setup(ctx => {
    Information("Running tasks...");

    if(AppVeyor.IsRunningOnAppVeyor)
    {
        AppVeyor.UpdateBuildVersion(versionInfo.SemVer);
    }
});

Teardown(ctx => Information("Finished running tasks."));

///////////////////////////////////////////////////////////////////////////////
// Configuration
///////////////////////////////////////////////////////////////////////////////
var papercutBinDir = "../src/Papercut.UI/bin";
var papercutServiceBinDir = "../src/Papercut.Service/bin";
var webUiTestsBinDir = "../test/Papercut.Module.WebUI.Tests/bin";

var outputDirectory = DirectoryPath.FromString("../out");

if (!DirectoryExists(outputDirectory))
{
    Information("Creating output directory {0}", outputDirectory);
    CreateDirectory(outputDirectory);
}
else {
    CleanDirectory(outputDirectory);
}

var appBuildDir = Directory(papercutBinDir) + Directory(configuration);
var svcBuildDir = Directory(papercutServiceBinDir) + Directory(configuration);
var testBuildDir = Directory(webUiTestsBinDir) + Directory(configuration);

///////////////////////////////////////////////////////////////////////////////
// TASKS
///////////////////////////////////////////////////////////////////////////////
Task("Clean")
    .Does(() =>
{
    CleanDirectory(appBuildDir);
    CleanDirectory(svcBuildDir);
    CleanDirectory(testBuildDir);

    foreach (var directory in GetDirectories("../src/Papercut.Module.*")) {
        var pluginOutputDir = directory.Combine(Directory("./bin/" + configuration));

        CleanDirectory(pluginOutputDir);
    }
});

///////////////////////////////////////////////////////////////////////////////
Task("Restore")
    .IsDependentOn("Clean")
    .Does(() =>
{
    NuGetRestore("../Papercut.sln");
});

///////////////////////////////////////////////////////////////////////////////
Task("PatchAssemblyInfo")
    .Does(() =>
{
    GitVersion(new GitVersionSettings{
        UpdateAssemblyInfo = true,
        OutputType = GitVersionOutput.BuildServer,
        UpdateAssemblyInfoFilePath = "../src/GlobalAssemblyInfo.cs"
    });
})
.OnError(exception => Error(exception));

///////////////////////////////////////////////////////////////////////////////
Task("CreateReleaseNotes")
    .Does(() => ReleaseNotes.Create(Context))
    .OnError(exception => Error(exception));

///////////////////////////////////////////////////////////////////////////////
Task("Build")
    .IsDependentOn("Restore")
    .Does(() =>
{
    MSBuild("../Papercut.sln", settings => settings
                            .SetConfiguration(configuration)
                            .SetVerbosity(Verbosity.Normal)
                            .SetPlatformTarget(PlatformTarget.MSIL)
                            .SetMSBuildPlatform(MSBuildPlatform.Automatic)
                            .UseToolVersion(MSBuildToolVersion.Default)
                            .WithTarget("Build"));
})
.OnError(exception => Error(exception));

///////////////////////////////////////////////////////////////////////////////
Task("Test")
    .IsDependentOn("Build")
    .Does(() =>
{
    NUnit3("../test/**/bin/" + configuration + "/*.Test.dll", new NUnit3Settings()); // { NoResults = true });

    if(AppVeyor.IsRunningOnAppVeyor)
    {
        AppVeyor.UploadTestResults("TestResult.xml", AppVeyorTestResultsType.NUnit3);
    }
})
.OnError(exception => Error(exception));

///////////////////////////////////////////////////////////////////////////////
Task("Package")
    .Does(() =>
{
    // remove the apppublish directory
    var publishDir = appBuildDir + Directory("./app.publish");
    DeleteDirectory(publishDir, new DeleteDirectorySettings { Recursive = true, Force = true });

    var appFileName = outputDirectory.CombineWithFilePath(string.Format("Papercut.{0}.zip", versionInfo.FullSemVer));
    Zip(appBuildDir, appFileName, GetFiles(appBuildDir.ToString() + "/**/*"));

    var svcFileName = outputDirectory.CombineWithFilePath(string.Format("PapercutService.{0}.zip", versionInfo.FullSemVer));
    Zip(svcBuildDir, svcFileName, GetFiles(svcBuildDir.ToString() + "/**/*"));

    var chocolateyFileName = outputDirectory.CombineWithFilePath(string.Format("papercut.{0}.nupkg", versionInfo.NuGetVersion));
    ChocolateyPack(
        File("../chocolatey/Papercut.nuspec"),
        new ChocolateyPackSettings {
            Version = versionInfo.NuGetVersion,
            OutputDirectory = outputDirectory
        });

    if(AppVeyor.IsRunningOnAppVeyor)
    {
        AppVeyor.UploadArtifact(appFileName);
        AppVeyor.UploadArtifact(svcFileName);
        AppVeyor.UploadArtifact("../src/Papercut.Bootstrapper/bin/" + configuration + "/Papercut.Setup.exe");
        AppVeyor.UploadArtifact(chocolateyFileName);
    }
})
.OnError(exception => Error(exception));

///////////////////////////////////////////////////////////////////////////////
Task("All")
    .IsDependentOn("PatchAssemblyInfo")
    .IsDependentOn("Clean")
    .IsDependentOn("CreateReleaseNotes")
    .IsDependentOn("Restore")
    .IsDependentOn("Build")
    .IsDependentOn("Test")
    .IsDependentOn("Package")
    .OnError(exception => Error(exception));

RunTarget(target);
