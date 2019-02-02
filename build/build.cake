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
const int MinorVersion = 3;

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
    CreateAssemblyInfo("../src/GlobalAssemblyInfo.cs", new AssemblyInfoSettings {
        Version = information.AssemblyVersion,
        FileVersion = information.FileVersion,
        InformationalVersion = information.SemanticVersion
    });   

    if(AppVeyor.IsRunningOnAppVeyor)
    {
        AppVeyor.UpdateBuildVersion(information.FileVersion);
    }
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
Task("CopyPlugins")
	.Does(() => 
{
    foreach (var directory in GetDirectories("../src/Papercut.Module.*")) {
        var pluginOutputDir = directory.Combine(Directory("./bin/" + configuration));

        Information("Copying Plugin in Directory {0} to App and Service...", pluginOutputDir);

        CopyDirectory(pluginOutputDir, appBuildDir);
        CopyDirectory(pluginOutputDir, svcBuildDir);
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

    var appFileName = outputDirectory.CombineWithFilePath(string.Format("Papercut.{0}.zip", information.FileVersion));
    Zip(appBuildDir, appFileName, GetFiles(appBuildDir.ToString() + "/**/*"));

    var svcFileName = outputDirectory.CombineWithFilePath(string.Format("PapercutService.{0}.zip", information.FileVersion));
    Zip(svcBuildDir, svcFileName, GetFiles(svcBuildDir.ToString() + "/**/*"));

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
    //.IsDependentOn("PatchAssemblyInfo")
    .IsDependentOn("Clean")
    .IsDependentOn("CreateReleaseNotes")
    .IsDependentOn("Restore")
    .IsDependentOn("Build")
    .IsDependentOn("Test")
	.IsDependentOn("CopyPlugins")
    .IsDependentOn("Package")
    .OnError(exception => Error(exception));

RunTarget(target);