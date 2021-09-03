#tool "nuget:?package=MarkdownSharp&version=1.13.0"
#tool "nuget:?package=MimekitLite&version=2.13.0 "
#tool "nuget:?package=NUnit.ConsoleRunner&version=3.9.0"
#tool "nuget:?package=OpenCover&version=4.6.519"
#tool "nuget:?package=GitVersion.CommandLine&version=5.7.0"

#addin "nuget:?package=Cake.FileHelpers&version=4.0.1"
#addin "nuget:?package=Cake.Incubator&version=6.0.0"

#reference "tools/MarkdownSharp.1.13.0.0/lib/35/MarkdownSharp.dll"
#reference "tools/MimeKitLite.2.13.0/lib/net45/MimeKitLite.dll"

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

var x64Dir = Directory("x64");
var x32Dir = Directory("x86");
var cfgDir = Directory(configuration);

var appBuildDir64 = Directory(papercutBinDir) + x64Dir + cfgDir;
var appBuildDir32 = Directory(papercutBinDir) + x32Dir + cfgDir;
var svcBuildDir = Directory(papercutServiceBinDir) + cfgDir;
var testBuildDir = Directory(webUiTestsBinDir) + x64Dir + cfgDir;

///////////////////////////////////////////////////////////////////////////////
// TASKS
///////////////////////////////////////////////////////////////////////////////
Task("Clean")
    .Does(() =>
{
    CleanDirectory(appBuildDir64);
    CleanDirectory(appBuildDir32);
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
// Assembly Version Patching
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
// RELEASE NOTES
Task("CreateReleaseNotes")
    .Does(() => ReleaseNotes.Create(Context))
    .OnError(exception => Error(exception));

///////////////////////////////////////////////////////////////////////////////
// BUILD
Task("Build64")
    .IsDependentOn("Restore")
    .Does(() =>
{
    MSBuild("../Papercut.sln", settings => settings
                            .SetConfiguration(configuration)
                            .SetVerbosity(Verbosity.Normal)
                            .SetPlatformTarget(PlatformTarget.x64)
                            .SetMSBuildPlatform(MSBuildPlatform.Automatic)
                            .UseToolVersion(MSBuildToolVersion.Default)
                            .WithTarget("Build"));
})
.OnError(exception => Error(exception));

Task("Build32")
    .IsDependentOn("Restore")
    .Does(() =>
{
    MSBuild("../Papercut.sln", settings => settings
                            .SetConfiguration(configuration)
                            .SetVerbosity(Verbosity.Normal)
                            .SetPlatformTarget(PlatformTarget.x86)
                            .SetMSBuildPlatform(MSBuildPlatform.Automatic)
                            .UseToolVersion(MSBuildToolVersion.Default)
                            .WithTarget("Build"));
})
.OnError(exception => Error(exception));

///////////////////////////////////////////////////////////////////////////////
// TESTS
Task("Test")
    .IsDependentOn("Build64")
    .Does(() =>
{
    NUnit3("../test/**/bin/x64/" + configuration + "/*.Test.dll", new NUnit3Settings()); // { NoResults = true });

    if(AppVeyor.IsRunningOnAppVeyor)
    {
        AppVeyor.UploadTestResults("TestResult.xml", AppVeyorTestResultsType.NUnit3);
    }
})
.OnError(exception => Error(exception));

///////////////////////////////////////////////////////////////////////////////
// PACKAGE STEPS

void MaybeUploadArtifact(FilePath fileName) {
    if(AppVeyor.IsRunningOnAppVeyor)
    {
        Information("Uploading Artifact to AppVeyor: " + fileName);   
        AppVeyor.UploadArtifact(fileName);
    }
}

Task("PackagePapercut64")
    .Does(() =>
{
    // remove the apppublish directory
    var publishDir = appBuildDir64 + Directory("./app.publish");    
    DeleteDirectory(publishDir, new DeleteDirectorySettings { Recursive = true, Force = true });

    var appFileName = outputDirectory.CombineWithFilePath(string.Format("Papercut.Smtp.x64.{0}.zip", versionInfo.FullSemVer));
    Zip(appBuildDir64, appFileName, GetFiles(appBuildDir64.ToString() + "/**/*"));
    MaybeUploadArtifact(appFileName);

    var chocolateyFileName = outputDirectory.CombineWithFilePath(string.Format("papercut.{0}.nupkg", versionInfo.NuGetVersion));
    ChocolateyPack(
        File("../chocolatey/Papercut.nuspec"),
        new ChocolateyPackSettings {
            Version = versionInfo.NuGetVersion,
            OutputDirectory = outputDirectory
        });
    
    MaybeUploadArtifact(chocolateyFileName);
})
.OnError(exception => Error(exception));

Task("PackagePapercut32")
    .Does(() =>
{
    // remove the apppublish directory
    var publishDir = appBuildDir32 + Directory("./app.publish");
    DeleteDirectory(publishDir, new DeleteDirectorySettings { Recursive = true, Force = true });

    var appFileName = outputDirectory.CombineWithFilePath(string.Format("Papercut.Smtp.x86.{0}.zip", versionInfo.FullSemVer));
    Zip(appBuildDir32, appFileName, GetFiles(appBuildDir32.ToString() + "/**/*"));
    MaybeUploadArtifact(appFileName);
})
.OnError(exception => Error(exception));

Task("PackagePapercutService")
    .Does(() =>
{    
    var svcFileName = outputDirectory.CombineWithFilePath(string.Format("Papercut.Smtp.Service.{0}.zip", versionInfo.FullSemVer));
    Zip(svcBuildDir, svcFileName, GetFiles(svcBuildDir.ToString() + "/**/*"));
    MaybeUploadArtifact(svcFileName);

})
.OnError(exception => Error(exception));

Task("PackageSetup")
    .Does(() =>
{
    MaybeUploadArtifact("../src/Papercut.Bootstrapper/bin/x64/" + configuration + "/Papercut.Smtp.Setup.exe");
    MaybeUploadArtifact("../src/Papercut.Bootstrapper/bin/x86/" + configuration + "/Papercut.Smtp.Setup.exe");
})
.OnError(exception => Error(exception));

///////////////////////////////////////////////////////////////////////////////
Task("All")
    .IsDependentOn("PatchAssemblyInfo")
    .IsDependentOn("Clean")
    .IsDependentOn("CreateReleaseNotes")
    .IsDependentOn("Restore")
    .IsDependentOn("Build32")
    .IsDependentOn("Build64")
    .IsDependentOn("Test")
    .IsDependentOn("PackagePapercut64")
    .IsDependentOn("PackagePapercut32")
    .IsDependentOn("PackagePapercutService")
    .IsDependentOn("PackageSetup")
    .OnError(exception => Error(exception));

RunTarget(target);
