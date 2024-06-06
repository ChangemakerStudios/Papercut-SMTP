public static class Velopack
{
    public static void Pack(ICakeContext context, VpkPackParams @params)
    {
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        var arguments = new ProcessArgumentBuilder()
            .Append("pack")
            .Append("-u").Append(@params.Id)
            .Append("--packTitle").AppendQuoted(@params.Title)
            .Append("--icon").AppendQuoted(@params.Icon)
            .Append("--releaseNotes").Append(@params.ReleaseNotes)
            .Append("--channel").Append(@params.Channel)
            .Append("-v").AppendQuoted(@params.Version)
            .Append("-p").AppendQuoted(@params.PublishDirectory)
            .Append("-o").AppendQuoted(@params.ReleaseDirectory)
            .Append("-e").AppendQuoted(@params.ExeName);

        if (!string.IsNullOrEmpty(@params.Framework))
        {
            arguments.Append("--framework").AppendQuoted(@params.Framework);
        }

        //Information("Running Vpk Pack with arguments: " + arguments.Render());
        context.StartProcess("vpk", new ProcessSettings
        {
            Arguments = arguments
        });
    }

    public static void UploadGithub(ICakeContext context, VpkUploadParams @params)
    {
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        var arguments = new ProcessArgumentBuilder()
            .Append("upload").Append("github")
            .Append("--outputDir").AppendQuoted(@params.ReleaseDirectory)
            .Append("--channel").AppendQuoted(@params.Channel)
            .Append("--repoUrl").AppendQuoted(@params.Repository)
            .Append("--token").AppendQuoted(@params.Token);

        if (@params.IsPrelease)
        {
            arguments.Append("--pre");
        }

        if (@params.Merge)
        {
            arguments.Append("--merge");
        }

        context.StartProcess("vpk", new ProcessSettings
        {
            Arguments = arguments
        });
    }
}

public class VpkUploadParams
{
    public string? ReleaseDirectory { get; set; }
    public string? Channel { get; set; }
    public string? Token { get; set; }
    public string? Repository { get; set; }
    public bool IsPrelease { get; set; } = false;
    public bool Merge { get; set; } = true;
}

public class VpkPackParams
{
    public string? Id { get; set; }
    public string? Title { get; set; }
    public string? Icon { get; set; }
    public string? ReleaseNotes { get; set; }
    public string? Channel { get; set; }
    public string? Version { get; set; }
    public string? PublishDirectory { get; set; }
    public string? ReleaseDirectory { get; set; }
    public string? ExeName { get; set; }
    public string? Framework { get; set; }
}