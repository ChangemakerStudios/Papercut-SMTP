#tool "nuget:?package=MarkdownSharp&version=1.13.0"
#tool "nuget:?package=MimekitLite&version=2.0.6"
#tool "nuget:?package=NUnit.ConsoleRunner&version=3.9.0"
#tool "nuget:?package=OpenCover&version=4.6.519"

#reference "tools/MarkdownSharp.1.13.0.0/lib/35/MarkdownSharp.dll"
#reference "tools/MimeKitLite.2.0.6/lib/net45/MimeKitLite.dll"

#addin "Cake.FileHelpers"

#load "./BuildInformation.cake" 
#load "./ReleaseNotes.cake" 

///////////////////////////////////////////////////////////////////////////////
// ARGUMENTS
///////////////////////////////////////////////////////////////////////////////

var target = Argument("target", "All");
var configuration = Argument("configuration", "Release");
var information = BuildInformation.GetBuildInformation(Context, BuildSystem);

const int MajorVersion = 5;
const int MinorVersion = 1;

///////////////////////////////////////////////////////////////////////////////
// SETUP / TEARDOWN
///////////////////////////////////////////////////////////////////////////////

Setup(ctx => {
    Information("Running tasks...");
    information.SetBuildVersion(ctx, MajorVersion, MinorVersion); 
});

Teardown(ctx => Information("Finished running tasks."));

///////////////////////////////////////////////////////////////////////////////
// Configuration
///////////////////////////////////////////////////////////////////////////////
var appBuildDir = Directory("../src/Papercut.UI/bin") + Directory(configuration);
var svcBuildDir = Directory("../src/Papercut.Service/bin") + Directory(configuration);
var testBuildDir = Directory("../test/Papercut.Module.WebUI.Tests/bin") + Directory(configuration);

///////////////////////////////////////////////////////////////////////////////
// TASKS
///////////////////////////////////////////////////////////////////////////////
Task("Clean")
    .Does(() =>
{
    CleanDirectory(appBuildDir);
    CleanDirectory(svcBuildDir);
    CleanDirectory(testBuildDir);
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
    CreateAssemblyInfo("../src/GlobalAssemblyInfo.cs", new AssemblyInfoSettings {
        Version = information.AssemblyVersion,
        FileVersion = information.FileVersion,
        InformationalVersion = information.SemanticVersion
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
    NUnit3("../test/**/bin/" + configuration + "/*.Test.dll", 
                    new NUnit3Settings()); // { NoResults = true });
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
    var appFileName = string.Format("Papercut.{0}.zip", information.FileVersion);
    var directory = "../src/Papercut.UI/bin/" + configuration;
    Zip(directory, appFileName, GetFiles(directory + "/**/*"));

    var svcFileName = string.Format("PapercutService.{0}.zip", information.FileVersion);
    directory = "../src/Papercut.Service/bin/" + configuration;
    Zip(directory, svcFileName, GetFiles(directory + "/**/*"));

    if(AppVeyor.IsRunningOnAppVeyor)
    {
        AppVeyor.UploadArtifact(appFileName);    
        AppVeyor.UploadArtifact(svcFileName);    
        AppVeyor.UploadArtifact("../src/Papercut.Bootstrapper/bin/" + configuration + "/Papercut.Setup.exe");    
    }

//   # Chocolately
//   - nuget pack chocolately\Papercut.nuspec -version %APPVEYOR_BUILD_VERSION%
//   - appveyor PushArtifact Papercut.%APPVEYOR_BUILD_VERSION%.nupkg
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