# Papercut v8 Desktop Plan

Status: draft, 2026-09-27. Tracks the "Papercut SMTP v8" board: https://github.com/orgs/ChangemakerStudios/projects/2

## Goal

One application, run three ways, on Windows, macOS and Linux:

- **Papercut.Service** is the app: SMTP, HTTP API, Angular UI, rules, MCP.
- **The tray** is the shell: tray icon, notifications, run-at-startup, and a window that shows the web UI.
- **Papercut.UI (WPF)** and **Papercut.Infrastructure.IPComm** are deleted.

v7 desktop users must get v8 as a normal in-place update, with their messages and settings intact. Rules come through intact if they were saved on the bridge release or later; see [Migration](#migration) for the one-time loss before that.

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

- Done: the WPF `RuleService` saves `rules.json` to `UserAppDataDirectory`. On load, if that file is missing and `BaseDirectory\rules.json` exists, `RuleServiceBase` copies it over and logs it. The service keeps `BaseDirectory`.
- `JsonSettingStore` is not part of the bridge. The WPF app keeps its real settings in `user.config` and only writes an empty `Papercut SMTP.Settings.json`. The service's settings file moves to the data folder in card B.
- Verified by running the WPF app: the legacy `rules.json` was copied on first load, and on exit an edited rule saved to `%AppData%` while the legacy file stayed unchanged.

**Limit:** Velopack replaces `current\` before any code from the new version runs. So for Velopack installs the copy finds nothing, and rules saved before the bridge are lost at that update, as they already are on every v7 update. The copy only helps zip, portable and dev runs. What the bridge does fix is that rules survive every update from this release on, including the update to v8.

Nothing can capture those files first: the v7 already installed has no pre-update hook, and Velopack's updater does the replacement. So the guarantee is narrowed on purpose:
- **Velopack desktop-only users** lose rules saved before the bridge, once, at the bridge update. This is no worse than today, because every v7 update already deletes them. The bridge release notes tell users to re-create their forwarding rules once after updating; they persist from then on.
- **Users who also ran the Windows Service** can get them back through the v8 fallback below.
- **Messages and settings are not affected.** They already live outside `current\`: messages in `%AppData%`, and WPF settings in `user.config` under `%LocalAppData%\Changemaker_Studios`.

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

- **Rules:** read from the data folder, where the bridge release put them. If there are none, import from the installed Windows Service's `rules.json`, found through the registry `ImagePath`. When both were running, the WPF app synced its rules to the service, so that copy may still exist.

## Work breakdown

| Card | Scope | Depends on |
|---|---|---|
| **0. Bridge release** | WPF rules in the data folder + copy from the old location (#383). Ship as 7.x. | none |
| **A. Cross-platform shell** | Avalonia 12 tray + webview window, replacing the WinForms tray. Absorbs "Desktop shell: cross-platform later". | Spike |
| **B. Desktop mode** | Tray launches the service as a child; source resolution; data-folder config. | A |
| **C. Velopack desktop package** | Package layout, update flow, v8 migration, new channels. | A, B, 0 |
| Wind down WPF | Existing card | A, B, C |
| Remove IPComm | Existing card | Wind down WPF |

### Spike (go/no-go for Avalonia)

Run on Windows, macOS and Linux:

Pin the `NativeWebView` package version the spike runs against.

1. **Must pass on all three:** intercept `NewWindowRequested`, read its URL, and suppress the popup. Email links arrive as `target="_blank"` new-window requests from the sandboxed iframe (`content-formatting.service.ts`).
2. **Top-level cancellation:** `NavigationStarted` can be cancelled only where `Features.Supports(NativeWebViewFeature.NavigationCancellation)` is true. The docs list Windows and embedded macOS, not Linux. Required on Windows and macOS. A Linux gap is acceptable only if item 3 holds, because the iframe sandbox has no `allow-top-navigation`, so email cannot navigate the top page.
3. **Iframe navigation:** frame loads raise no navigation events on any platform, so the defence goes into the email content, not the webview:
   - the renderer forces `target="_blank"` on every link and strips `<meta http-equiv="refresh">`
   - the sandbox stays without `allow-scripts`, `allow-forms` or `allow-top-navigation`
   - the spike proves that a `target="_self"` link, a meta refresh and a form post in an email do not navigate the iframe on any platform
4. Turn devtools off (`IsDevToolsEnabled`) and set the user data folder. No permission-request event is documented, so check what each platform does by default.
5. Self-contained publish + `vpk pack` works on each OS.
6. **Linux:** install and launch the package on a clean supported distro. Confirm the WPE or WebKitGTK prerequisites (GTK 3, WebKitGTK 4.1, libsoup 3) are bundled or documented.
7. An OS notification route exists on each OS: Windows toast, macOS UserNotifications, freedesktop D-Bus.
8. Confirm the `NativeWebView` package license.

If 1 or 3 fails, or 2 fails on Windows or macOS, switch to Tauri.

### A. Shell details

- One window, reused; closing hides it to the tray.
- Menu: "Open Papercut" is the default item (also on double-click and notification click). "Open in Browser" is secondary.
- Link policy: the service origin loads in the window. Other http(s) links open in the default browser, and `mailto:` goes to the OS handler. Everything else is blocked and logged. The origin check applies to every navigation and new-window request, whether or not the user started it.
- Notification click opens `message/{id}`. If the window is already open, route in place with a web message and a small Angular listener, instead of reloading.
- Offline page with Start/Retry buttons; it reloads when the SignalR hub reconnects.
- Platform services behind interfaces:
  - run-at-startup: registry on Windows, LaunchAgent on macOS, XDG autostart on Linux
  - service control: Windows only
  - notifications
- Carry over from Papercut.UI: the `DisableEdgeFeaturesHelper` settings and the `WebView2Information` runtime check. Port the link handling from `MessageDetailHtmlViewModel`, with one change: it allows every navigation the user did not start (`!args.IsUserInitiated`) without checking the URL. The shell drops that exception, and the origin check above always applies. The `HtmlPreviewVisitor` MIME edge cases belong to the WPF wind-down card.

### B. Desktop mode details

- Launch `service/Papercut.Service` with an explicit `--urls` and data folder, so the tray knows the URL without guessing.
- Stopping: the service exits when stdin closes or its parent process disappears; this works on every OS. On Windows, a Job Object also kills it if the tray crashes.
- The default SMTP port on macOS/Linux is 2525, because ports below 1024 need root.
- The status line shows the source: "Windows Service", "Local", or "External at …". Start/stop target the active source and are disabled for external services.

## Open questions

- Apple Developer ID for notarization, or ship the first macOS build unsigned?
- Confirm the two *(proposed)* defaults under Model.
