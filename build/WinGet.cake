#addin nuget:?package=Cake.Yaml&version=6.0.0
#addin nuget:?package=YamlDotNet&version=12.3.1

public static class WinGet
{
    public static void PrepareRelease(ICakeContext context, WinGetReleaseParams @params)
    {
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        if (string.IsNullOrWhiteSpace(@params.Version))
        {
            throw new ArgumentException("WinGet: Version is required.", nameof(@params));
        }

        if (@params.ReleasesDirectory == null || @params.OutputDirectory == null)
        {
            throw new ArgumentException("WinGet: ReleasesDirectory and OutputDirectory are required.", nameof(@params));
        }

        context.Information($"Preparing WinGet manifests for version {@params.Version}");

        // Ensure output directory exists
        var wingetOutputDir = @params.OutputDirectory;
        context.EnsureDirectoryExists(wingetOutputDir);

        // Calculate SHA256 hashes for installers
        var architectures = new[] { "x64", "x86", "arm64" };
        var installerHashes = new Dictionary<string, string>();

        foreach (var arch in architectures)
        {
            var setupFileName = $"PapercutSMTP-win-{arch}{@params.ChannelPostfix}-Setup.exe";
            var setupFilePath = @params.ReleasesDirectory.CombineWithFilePath(setupFileName);

            if (context.FileExists(setupFilePath))
            {
                context.Information($"Calculating SHA256 for {setupFileName}");
                var hash = context.CalculateFileHash(setupFilePath, HashAlgorithm.SHA256).ToHex().ToUpperInvariant();
                installerHashes[arch] = hash;
                context.Information($"  {arch}: {hash}");
            }
            else
            {
                context.Warning($"Setup file not found: {setupFilePath}");
            }
        }

        // Fail-fast check: ensure at least one installer was found
        if (installerHashes.Count == 0)
        {
            var errorMessage = $"No installer files found in {@params.ReleasesDirectory}. " +
                             $"Expected files: PapercutSMTP-win-{{x64,x86,arm64}}{@params.ChannelPostfix}-Setup.exe. " +
                             "WinGet manifest generation requires at least one installer to be present.";
            context.Error(errorMessage);
            throw new InvalidOperationException(errorMessage);
        }

        context.Information($"Found {installerHashes.Count} installer(s) for WinGet manifest generation");

        // Generate version manifest
        var versionManifest = new WinGetVersionManifest
        {
            PackageIdentifier = @params.PackageIdentifier,
            PackageVersion = @params.Version,
            DefaultLocale = "en-US",
            ManifestType = "version",
            ManifestVersion = "1.6.0"
        };

        var versionFilePath = wingetOutputDir.CombineWithFilePath($"{@params.PackageIdentifier}.yaml");
        context.SerializeYamlToFile(versionFilePath, versionManifest);
        context.Information($"Generated: {versionFilePath.GetFilename()}");

        // Generate locale manifest
        var localeManifest = new WinGetLocaleManifest
        {
            PackageIdentifier = @params.PackageIdentifier,
            PackageVersion = @params.Version,
            PackageLocale = "en-US",
            Publisher = "Changemaker Studios",
            PublisherUrl = "https://github.com/ChangemakerStudios",
            PublisherSupportUrl = "https://github.com/ChangemakerStudios/Papercut-SMTP/issues",
            Author = "Ken Robertson & Jaben Cargman",
            PackageName = "Papercut SMTP",
            PackageUrl = "https://github.com/ChangemakerStudios/Papercut-SMTP",
            License = "Apache-2.0",
            LicenseUrl = "https://github.com/ChangemakerStudios/Papercut-SMTP/blob/develop/LICENSE",
            Copyright = $"Copyright © 2008 - {DateTime.UtcNow.Year} Ken Robertson & Jaben Cargman",
            ShortDescription = "Standalone SMTP server designed for viewing received messages",
            Description = "Papercut SMTP is a 2-in-1 quick email viewer AND built-in SMTP server designed for development.\n" +
                         "It allows developers to safely test email functionality without risk of emails being sent to real recipients.\n\n" +
                         "Features:\n" +
                         "- Desktop WPF application for viewing emails\n" +
                         "- Built-in SMTP server (receive-only)\n" +
                         "- Full email inspection (body, HTML, headers, attachments, raw encoded bits)\n" +
                         "- Support for running as a minimized tray application with notifications\n" +
                         "- WebView2-based HTML email rendering",
            Moniker = "papercut-smtp",
            Tags = new List<string>
            {
                "smtp",
                "email",
                "development",
                "testing",
                "developer-tools",
                "mail-server"
            },
            ReleaseNotes = $"https://github.com/ChangemakerStudios/Papercut-SMTP/releases/tag/{@params.Version}",
            ReleaseNotesUrl = $"https://github.com/ChangemakerStudios/Papercut-SMTP/releases/tag/{@params.Version}",
            ManifestType = "defaultLocale",
            ManifestVersion = "1.6.0"
        };

        var localeFilePath = wingetOutputDir.CombineWithFilePath($"{@params.PackageIdentifier}.locale.en-US.yaml");
        context.SerializeYamlToFile(localeFilePath, localeManifest);
        context.Information($"Generated: {localeFilePath.GetFilename()}");

        // Generate installer manifest
        var installerManifest = new WinGetInstallerManifest
        {
            PackageIdentifier = @params.PackageIdentifier,
            PackageVersion = @params.Version,
            Platform = new List<string> { "Windows.Desktop" },
            MinimumOSVersion = "10.0.17763.0",
            InstallerType = "exe",
            Scope = "user",
            InstallModes = new List<string> { "interactive", "silent", "silentWithProgress" },
            InstallerSwitches = new WinGetInstallerSwitches
            {
                Silent = "--silent",
                SilentWithProgress = "--silent"
            },
            UpgradeBehavior = "install",
            Installers = new List<WinGetInstaller>(),
            ManifestType = "installer",
            ManifestVersion = "1.6.0"
        };

        // Add installers for each architecture
        foreach (var arch in architectures)
        {
            if (installerHashes.ContainsKey(arch))
            {
                installerManifest.Installers.Add(new WinGetInstaller
                {
                    Architecture = arch,
                    InstallerUrl = $"https://github.com/ChangemakerStudios/Papercut-SMTP/releases/download/{@params.Version}/PapercutSMTP-win-{arch}{@params.ChannelPostfix}-Setup.exe",
                    InstallerSha256 = installerHashes[arch]
                });
            }
        }

        var installerFilePath = wingetOutputDir.CombineWithFilePath($"{@params.PackageIdentifier}.installer.yaml");
        context.SerializeYamlToFile(installerFilePath, installerManifest);
        context.Information($"Generated: {installerFilePath.GetFilename()}");

        context.Information($"WinGet manifests prepared successfully in: {wingetOutputDir}");
    }
}

// Parameter class
public class WinGetReleaseParams
{
    public string PackageIdentifier { get; set; } = "ChangemakerStudios.PapercutSMTP";
    public string? Version { get; set; }
    public string? ChannelPostfix { get; set; }
    public DirectoryPath? ReleasesDirectory { get; set; }
    public DirectoryPath? OutputDirectory { get; set; }
}

// Manifest model classes
public class WinGetVersionManifest
{
    public string? PackageIdentifier { get; set; }
    public string? PackageVersion { get; set; }
    public string? DefaultLocale { get; set; }
    public string? ManifestType { get; set; }
    public string? ManifestVersion { get; set; }
}

public class WinGetLocaleManifest
{
    public string? PackageIdentifier { get; set; }
    public string? PackageVersion { get; set; }
    public string? PackageLocale { get; set; }
    public string? Publisher { get; set; }
    public string? PublisherUrl { get; set; }
    public string? PublisherSupportUrl { get; set; }
    public string? Author { get; set; }
    public string? PackageName { get; set; }
    public string? PackageUrl { get; set; }
    public string? License { get; set; }
    public string? LicenseUrl { get; set; }
    public string? Copyright { get; set; }
    public string? ShortDescription { get; set; }
    public string? Description { get; set; }
    public string? Moniker { get; set; }
    public List<string>? Tags { get; set; }
    public string? ReleaseNotes { get; set; }
    public string? ReleaseNotesUrl { get; set; }
    public string? ManifestType { get; set; }
    public string? ManifestVersion { get; set; }
}

public class WinGetInstallerManifest
{
    public string? PackageIdentifier { get; set; }
    public string? PackageVersion { get; set; }
    public List<string>? Platform { get; set; }
    public string? MinimumOSVersion { get; set; }
    public string? InstallerType { get; set; }
    public string? Scope { get; set; }
    public List<string>? InstallModes { get; set; }
    public WinGetInstallerSwitches? InstallerSwitches { get; set; }
    public string? UpgradeBehavior { get; set; }
    public List<WinGetInstaller>? Installers { get; set; }
    public string? ManifestType { get; set; }
    public string? ManifestVersion { get; set; }
}

public class WinGetInstallerSwitches
{
    public string? Silent { get; set; }
    public string? SilentWithProgress { get; set; }
}

public class WinGetInstaller
{
    public string? Architecture { get; set; }
    public string? InstallerUrl { get; set; }
    public string? InstallerSha256 { get; set; }
}
