namespace Papercut.Shell.Spike;

/// <summary>
/// One line per webview decision, mirrored to a file so results can be collected from each OS.
/// </summary>
internal static class SpikeLog
{
    private static readonly object Sync = new();

    public static string FilePath { get; private set; } = "";

    public static event Action<string>? Line;

    public static void Start()
    {
        var os = OperatingSystem.IsWindows() ? "windows" : OperatingSystem.IsMacOS() ? "macos" : "linux";
        // not beside the exe: an installed .app bundle is read-only, and Velopack replaces it on update
        var folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Papercut.Shell.Spike",
            "logs");

        Directory.CreateDirectory(folder);
        FilePath = Path.Combine(folder, $"spike-results-{os}-{DateTime.Now:yyyyMMdd-HHmmss}.log");
    }

    public static void Write(string source, string verdict, string detail)
    {
        var line = $"{DateTime.Now:HH:mm:ss.fff}  {source,-26} {verdict,-10} {detail}";

        lock (Sync)
        {
            File.AppendAllText(FilePath, line + Environment.NewLine);
        }

        Console.WriteLine(line);
        Line?.Invoke(line);
    }
}
