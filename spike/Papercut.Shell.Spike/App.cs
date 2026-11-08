using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Platform;
using Avalonia.Themes.Fluent;

namespace Papercut.Shell.Spike;

/// <summary>
/// Tray icon + one reused window, the shape the real shell will have.
/// </summary>
internal sealed class App : Application
{
    private MainWindow? _window;

    public static Uri Origin { get; set; } = null!;

    public static bool EnableDevTools { get; set; }

    public override void Initialize()
    {
        Styles.Add(new FluentTheme());
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
        {
            base.OnFrameworkInitializationCompleted();
            return;
        }

        var open = new NativeMenuItem("Open Papercut");
        open.Click += (_, _) => ShowWindow();

        var exit = new NativeMenuItem("Exit");
        exit.Click += (_, _) =>
        {
            _window?.AllowClose();
            desktop.Shutdown();
        };

        var tray = new TrayIcon
        {
            Icon = new WindowIcon(AssetLoader.Open(new Uri("avares://Papercut.Shell.Spike/Assets/papercut.ico"))),
            ToolTipText = "Papercut shell spike",
            Menu = new NativeMenu { open, new NativeMenuItemSeparator(), exit }
        };
        tray.Clicked += (_, _) => ShowWindow();

        TrayIcon.SetIcons(this, new TrayIcons { tray });
        SpikeLog.Write("tray", "created", "left-click or 'Open Papercut' shows the window; 'Exit' quits");

        ShowWindow();

        base.OnFrameworkInitializationCompleted();
    }

    private void ShowWindow()
    {
        _window ??= new MainWindow(Origin, EnableDevTools);
        _window.Show();
        _window.Activate();
    }
}
