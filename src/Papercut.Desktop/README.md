# Papercut.Desktop (v8 shell)

The desktop app from v8 on: an Avalonia 12 tray icon and a window that shows the service's web UI in the OS webview (WebView2 on Windows, WKWebView on macOS). It builds as `Papercut.exe`, the v7 exe name. Design: [docs/plans/v8-desktop.md](../../docs/plans/v8-desktop.md).

## What it does

- **Picks a service**, first match wins: the installed Windows Service if it is running, then one already reachable (`PAPERCUT_SERVICE_URL`, `ExternalServiceUrl` in the shell settings, Docker on `127.0.0.1:8080`), then its own copy started as a child process from `service/`. A Windows Service that starts later takes over from the child, which frees the SMTP port for it. The child gets `--urls http://127.0.0.1:37410` (or a free port), `--DataDirectory`, and `--ParentProcessId`. It stops when the shell closes its stdin, exits, or (on Windows) is killed, through a job object.
- **Window**: opens on demand from the tray, and closing it hides it. The link policy ([LinkPolicy.cs](LinkPolicy.cs)) runs on every navigation and new-window request: the service origin stays in the window, http(s) and `mailto:` go to the OS, and everything else is blocked. On Windows, iframe navigations and permission prompts are hooked on the raw `CoreWebView2`.
- **Menus**: the tray menu, and a File / View / Options / Help menu bar on the window (the system menu bar on macOS) with the same actions plus Reload, Open Data Folder and Check for Updates. Menus are rebuilt only when their state changes.
- **Notifications**: new mail arrives over SignalR (`/hubs/messages`). Windows shows WinRT toasts under the v7 AUMID, and clicking one opens the message. macOS uses `osascript` for now (no click handling); UserNotifications needs the signed `.app`.
- **Run at startup**: on Windows, the v7 `HKCU\...\Run\Papercut.Smtp.App` value; on macOS, a LaunchAgent.
- **Updates**: Velopack from GitHub releases, on the channel the build was packed with. Updates download in the background. "Restart to Update" stops the child service first; otherwise the update is applied on Exit.
- **v7 import**, once: the newest WPF `user.config` gives the SMTP IP and port (written to the service settings) plus message paths, notifications and window size. v7's `rules.json` can't be recovered, because Velopack replaces `current\` before any v8 code runs. A Windows Service's copy is imported if there is one; otherwise a one-time notice asks the user to re-create their rules.

Data folder: `%AppData%\Changemaker Studios\Papercut SMTP` (the v7 one) on Windows, `~/Library/Application Support/Papercut SMTP` on macOS. Shell settings: `Papercut.Shell.json`. Logs: `Logs/`.

## Develop

```
dotnet build src/Papercut.Service      # Debug; the shell finds it at ../Papercut.Service/bin/Debug/net10.0
dotnet run --project src/Papercut.Desktop -f net10.0-windows10.0.19041.0
```

To run another instance, launch it again: a second launch shows the first instance's window and exits.

| Variable | Effect |
|---|---|
| `PAPERCUT_DATA_DIR` | Use this folder for everything, including messages, and skip the v7 import. Use it for test runs so your real data is not touched. |
| `PAPERCUT_SERVICE_EXE` | Run this `Papercut.Service` instead of `service/`. |
| `PAPERCUT_SERVICE_URL` | Connect to this service instead of starting one. |
| `PAPERCUT_DEVTOOLS=1` | Enable webview devtools. |
| `PAPERCUT_DEBUG=1` | Debug-level shell log, including the child's console output. |

## Package

- Windows: `dotnet-cake --target=PackageDesktop-win-x64` (also `-win-x86`, `-win-arm64`, or `PackageDesktop` for all three). This publishes the shell, publishes the service into `service/`, and runs `vpk pack` with id `PapercutSMTP`, the `webview2` prerequisite and the v7 AUMID. Output goes to `releases/`. The tasks are not part of `All` yet, so CI still ships the WPF app.
- macOS: `src/Papercut.Desktop/packaging/macos/pack.sh [osx-arm64|osx-x64] [version]` on a Mac. Builds unsigned for now.
