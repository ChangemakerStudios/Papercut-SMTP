# Papercut v8 Desktop Plan

Status: draft, 2026-09-27. Tracks the "Papercut SMTP v8" board: https://github.com/orgs/ChangemakerStudios/projects/2

## Goal

One application, run three ways, on Windows, macOS and Linux:

- **Papercut.Service** is the app: SMTP, HTTP API, Angular UI, rules, MCP.
- **The tray** is the shell: tray icon, notifications, run-at-startup, and a window that shows the web UI.
- **Papercut.UI (WPF)** and **Papercut.Infrastructure.IPComm** are deleted.

v7 desktop users must get v8 as a normal in-place update, with their messages, rules and settings intact.

## Model

Two independent choices: where the service comes from, and where the UI is shown.

**Service source.** The tray uses the first one available:

| # | Source | Tray's role | Platforms |
|---|---|---|---|
| 1 | Already reachable (Docker, console, remote) | Connect only | All |
| 2 | Installed Windows Service | Connect + start/stop/restart | Windows |
| 3 | Desktop: tray launches `Papercut.Service` as a child process | Owns its lifetime | All |

**UI surface:** the shell's window (native webview) or the default browser. Either works with any source.

Rules:

- Only one Papercut instance owns the SMTP port. The tray starts its own service only when #1 and #2 are not available. It stops that service before starting the Windows Service.
- A Windows Service that is installed but stopped falls back to #3 (no UAC prompt). The status line says so. *(proposed)*
- The window opens on demand, not when the tray starts. *(proposed)*

## Runtime: Avalonia 12

The tray is WinForms today, which only runs on Windows. It moves to **Avalonia 12**:

- `TrayIcon` works on Windows, macOS and Linux (StatusNotifierItem on Linux; GNOME needs the AppIndicator extension).
- `NativeWebView` is open source as of v12. It uses the OS webview (WebView2, WKWebView, WPE WebKit/WebKitGTK), so no Chromium is bundled.
- The framework is MIT licensed. Only the Accelerate tooling is paid, and Papercut does not need it.
- The output is a plain `dotnet publish`, so it fits the existing `vpk pack` flow.

Rejected:

- **Photino:** has no tray icon.
- **Electron/ElectronSharp:** bundles Chromium, which is past its support window. That matters for an app that renders untrusted email HTML.
- **Tauri:** the fallback if Avalonia fails the spike. It is the same architecture with a Rust shell.

## Data locations

Velopack replaces `current\` on every update (see [Preserving Files](https://docs.velopack.io/integrating/preserved-files)). Two files live there today and are lost on every update:

- `rules.json`: `RuleServiceBase` saves it to `AppDomain.BaseDirectory`. The WPF app uses the same class, so **v7 users probably lose their rules on every update today**.
- `<AppName>.Settings.json`: `JsonSettingStore` also saves it to `BaseDirectory`.

The desktop data folder is the existing `AppConstants.UserAppDataDirectory`, which is `%ApplicationData%\Changemaker Studios\Papercut SMTP`. That is already the first entry in the WPF `MessagePaths`. On macOS/Linux it is the equivalent `SpecialFolder.ApplicationData` path.

The Windows Service zip and Docker keep `BaseDirectory` so existing service installs do not move.

## Packaging

The **default install** is Velopack, containing the tray shell plus the desktop service.

| | v7 | v8 |
|---|---|---|
| Pack id | `PapercutSMTP` | `PapercutSMTP` (unchanged) |
| Channels | `win-x64`, `win-x86`, `win-arm64` | same, plus `osx-arm64`, `osx-x64`, `linux-x64` |
| Main exe | `Papercut.exe` (WPF) | `Papercut.exe` (tray shell) |
| AUMID | `ChangemakerStudios.PapercutSMTP` | unchanged |
| Framework | `net8.0-x64-desktop,webview2` | `webview2` only (self-contained) |
| Layout | WPF app | `Papercut.exe` + `service/` (Papercut.Service + web UI) |

Keeping the id, channels, exe name and AUMID means v7's `UpdateManager` offers v8 as an ordinary update. Shortcuts, taskbar pins, toast identity and the run-at-startup registry value keep working.

The shell calls `VelopackApp.Build().Run()` first, and uses the same `GithubSource` for updates. Before it applies an update, it stops the child service, because that service holds files in `current\`.

Secondary distributions are unchanged: the service zip (service + tray in `TrayNotification/`), Docker, and WinGet.

macOS packages must be built on a macOS runner, and they need an Apple Developer ID for notarization.

## Migration

### Part 1: 7.x bridge release (branch `fix/rules-settings-data-folder`)

Ships before v8 so rules are somewhere v8 can find them.

- `RuleServiceBase` saves `rules.json` to the data folder. On load, if that file is missing and `BaseDirectory\rules.json` exists, it copies it over.
- `JsonSettingStore` does the same for desktop installs only.
- Log the move once.

Users who jump from an older v7 straight to v8 lose rules they saved in `current\`. The bridge release shrinks that gap as much as possible.

### Part 2: v8 first-run migration

Runs in the shell on first start. It is idempotent and guarded by a marker file in the data folder.

- **Messages:** keep the WPF `MessagePaths` list as-is and pass it to the desktop service.
- **WPF settings:** read the newest `user.config` under `%LocalAppData%\Changemaker_Studios\Papercut.exe_Url_*\<version>\`.

| WPF setting | Goes to |
|---|---|
| `IP`, `Port` | Desktop service SMTP. The service defaults to 2525 and WPF to 25, so this must carry over. |
| `Forward*` | Service settings |
| `MessagePaths`, `LoggingPaths` | Service settings |
| `RunOnStartup`, `StartMinimized`, `MinimizeToTray`, `MinimizeOnClose` | Shell |
| `MainWindowHeight`/`Width`, `ShowNotifications` | Shell |
| `MessageListSortOrder`, `Theme`, `BaseTheme`, zoom | Web UI where an equivalent exists, otherwise dropped and noted in the release notes |
| `WebView2UserFolder`, `IgnoreSslCertificateErrors` | Dropped |

- **Rules:** read from the data folder, where the bridge release put them.

## Work breakdown

| Card | Scope | Depends on |
|---|---|---|
| **0. Bridge release** | Data-folder rules/settings + copy from old location. Ship as 7.x. | none |
| **A. Cross-platform shell** | Avalonia 12 tray + webview window, replacing the WinForms tray. Absorbs "Desktop shell: cross-platform later". | Spike |
| **B. Desktop mode** | Tray launches the service as a child; source resolution; data-folder config. | A |
| **C. Velopack desktop package** | Package layout, update flow, v8 migration, new channels. | A, B, 0 |
| Wind down WPF | Existing card | A, B, C |
| Remove IPComm | Existing card | Wind down WPF |

### Spike (go/no-go for Avalonia)

Run on Windows, macOS and Linux:

1. **Must pass:** cancel top-level and iframe navigations, and intercept new-window requests. Email links arrive as `target="_blank"` new-window requests from the sandboxed iframe (`content-formatting.service.ts`).
2. Turn devtools off, set the user data folder, deny permission requests.
3. Self-contained publish + `vpk pack` works on each OS.
4. An OS notification route exists on each OS: Windows toast, macOS UserNotifications, freedesktop D-Bus.
5. Confirm the `NativeWebView` package license.

If 1 fails, switch to Tauri.

### A. Shell details

- One window, reused; closing hides it to the tray.
- Menu: "Open Papercut" is the default item (also on double-click and notification click). "Open in Browser" is secondary.
- Link policy: the service origin loads in the window. Other http(s) links open in the default browser, and `mailto:` goes to the OS handler. Everything else is blocked and logged.
- Notification click opens `message/{id}`. If the window is already open, route in place with a web message and a small Angular listener, instead of reloading.
- Offline page with Start/Retry buttons; it reloads when the SignalR hub reconnects.
- Platform services behind interfaces:
  - run-at-startup: registry on Windows, LaunchAgent on macOS, XDG autostart on Linux
  - service control: Windows only
  - notifications
- Carry over from Papercut.UI: the `DisableEdgeFeaturesHelper` settings, the navigation and new-window policy from `MessageDetailHtmlViewModel`, and the `WebView2Information` runtime check. The `HtmlPreviewVisitor` MIME edge cases belong to the WPF wind-down card.

### B. Desktop mode details

- Launch `service/Papercut.Service` with an explicit `--urls` and data folder, so the tray knows the URL without guessing.
- Stopping: the service exits when stdin closes or its parent process disappears; this works on every OS. On Windows, a Job Object also kills it if the tray crashes.
- The default SMTP port on macOS/Linux is 2525, because ports below 1024 need root.
- The status line shows the source: "Windows Service", "Local", or "External at …". Start/stop target the active source and are disabled for external services.

## Open questions

- Apple Developer ID for notarization, or ship the first macOS build unsigned?
- Confirm the two *(proposed)* defaults under Model.
