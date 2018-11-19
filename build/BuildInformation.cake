public class BuildInformation
{
    public string Target { get; private set; }
    public string Configuration { get; private set; }
    
    public bool IsLocalBuild { get; private set; }
    public bool IsRunningOnAppVeyor { get; private set; }

    public bool IsPullRequest { get; private set; }
    public bool IsTagged { get; private set; }

    public int MajorVersion { get; set; }
    public int MinorVersion { get; set; }
    public int BuildVersion { get; set; }

    public string FileVersion { get; set; }
    public string AssemblyVersion { get; set; }
    public string SemanticVersion { get; set; }

    public void SetBuildVersion(ICakeContext context, int majorVersion, int minorVersion)
    {
        var appVeyor = context.BuildSystem().AppVeyor;
        var commitId = appVeyor.IsRunningOnAppVeyor ? appVeyor.Environment.Repository.Commit.Id : "LocalBuild";

        MajorVersion = majorVersion;
        MinorVersion = minorVersion;
        BuildVersion = appVeyor.IsRunningOnAppVeyor 
                            ? appVeyor.Environment.Build.Number 
                            : 0;
        FileVersion = string.Format("{0}.{1}.{2}.{3}", MajorVersion, MinorVersion, BuildVersion, 0);
        AssemblyVersion = string.Format("{0}.{1}.{2}.{3}", MajorVersion, MinorVersion, 0, 0);
        SemanticVersion = string.Format("{0} (Commit: {1}", FileVersion, commitId);
    }

    public static BuildInformation GetBuildInformation(ICakeContext context, BuildSystem buildSystem)
    {
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        if(buildSystem == null)
        {
            throw new ArgumentNullException(nameof(buildSystem));
        }
        var target = context.Argument("target", "Default");


        return new BuildInformation {
            Target = target,
            Configuration = context.Argument("configuration", "Release"),

            IsLocalBuild = buildSystem.IsLocalBuild,
            IsRunningOnAppVeyor = buildSystem.AppVeyor.IsRunningOnAppVeyor,
            IsPullRequest = buildSystem.AppVeyor.Environment.PullRequest.IsPullRequest,

            IsTagged = (
                buildSystem.AppVeyor.Environment.Repository.Tag.IsTag &&
                !string.IsNullOrWhiteSpace(buildSystem.AppVeyor.Environment.Repository.Tag.Name)
            )
        };
    }
}