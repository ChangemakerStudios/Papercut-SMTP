# Papercut shell spike (Avalonia 12)

Throwaway go/no-go spike for card A in [docs/plans/v8-desktop.md](../../docs/plans/v8-desktop.md). It is not part of `Papercut.sln`.

It is a tray icon plus one window hosting `NativeWebView` from `Avalonia.Controls.WebView` 12.1.0 (AvaloniaUI, MIT). The window loads a local test page that reproduces the web UI's email iframe: the same `sandbox` attributes and the same injected `<base target="_blank">`. Every navigation, popup and permission decision is logged to the window and to `spike-results-<os>-<time>.log` next to the exe.

## Run

```
dotnet run --project spike/Papercut.Shell.Spike            # add -- --devtools to allow devtools
```

Click every case on the page (T1–T7, then I1–I5 in the iframe, with Reload between iframe cases), then choose **Exit** from the tray menu. Collect the log file.

## Package: official vs community

Two packages exist. The spike uses the **official** one.

| | `Avalonia.Controls.WebView` 12.1.0 | `NativeWebView` 12.0.4.9 |
|---|---|---|
| Author | AvaloniaUI OÜ | Wiesław Šoltés (community) |
| License | MIT | MIT |
| Cancel navigation | `NavigationStarted` → `Cancel` | feature-flagged; not advertised on Linux |

What decompiling the official package shows (12.1.0):

| | Windows (WebView2) | macOS (WKWebView) | Linux (WPE) |
|---|---|---|---|
| `NavigationStarted` covers | main frame only (`NavigationStarting`) | all frames (`decidePolicyForNavigationAction`) | main frame and subframes (`decide-policy`, navigation action) |
| `Cancel` honoured | yes | yes | yes (`webkit_policy_decision_ignore`) |
| `NewWindowRequested` | `Handled` suppresses the popup | `Handled` suppresses the popup | popup is always suppressed |
| Iframe navigations | **not reported** — hook `FrameNavigationStarting` on the raw `CoreWebView2` from `TryGetPlatformHandle()` | reported via `NavigationStarted` | reported via `NavigationStarted` |
| Permission prompts | no Avalonia API — hook `PermissionRequested` on the raw `CoreWebView2` | not exposed | not exposed |
| Devtools / data folder | `EnableDevTools`, `UserDataFolder` | `EnableDevTools`, data store options | `EnableDevTools`; GTK: `BaseDataDirectory` |

The Linux GTK fallback adapter was not inspected.

## Results

### Windows 11 (26200), WebView2 153 — pass

Log: [results/windows-2026-09-27.log](results/windows-2026-09-27.log)

| Case | Result | Caught by |
|---|---|---|
| T1 same-origin link | allowed | `NavigationStarted` |
| T2 external link, same window | cancelled; page stays | `NavigationStarted` |
| T3 `target=_blank` | no popup, URL captured | `NewWindowRequested` |
| T4 `window.open` | no popup, URL captured | `NewWindowRequested` |
| T5 `mailto:` | cancelled | `NavigationStarted` |
| T6 `file:` link | never navigates | Chromium blocks it |
| T7 geolocation | denied | raw `PermissionRequested` |
| I1 email link (base `_blank`) | no popup, URL captured | `NewWindowRequested` |
| I2 email link `target=_self` | cancelled | raw `FrameNavigationStarting` only |
| I3 email link `target=_top` | never navigates | iframe sandbox |
| I4 email meta refresh | never navigates | iframe sandbox (sandboxed frames block meta refresh) |
| I5 email form post | never navigates | iframe sandbox (no `allow-forms`) |
| Tray + close-to-tray | works | |

Findings:

- **Iframe navigations need the raw WebView2 hook on Windows.** Avalonia's `NavigationStarted` never saw I2; `FrameNavigationStarting` on the raw `CoreWebView2` cancelled it. `CoreWebView2.CreateFromComICoreWebView2(handle.CoreWebView2)` works alongside Avalonia's own interop.
- **The sandbox does most of the work.** I3, I4 and I5 never produce a navigation, so the renderer-side defences in the plan are a second layer, not the only one.
- **An app manifest is required on Windows.** Without a Windows 10 `supportedOS` entry, the native control host throws "Unable to create child window".
- **The shell must enforce a single instance.** A second instance, or leftover `msedgewebview2` processes from a killed one, crashes on the locked user data folder with `0x800700AA`.

### macOS (M4 MacBook Pro), WKWebView — pass

Log: [results/macos-2026-09-27.log](results/macos-2026-09-27.log). The paste starts at `EnvironmentRequested`, so the tray lines are not in it.

| Case | Result | Caught by |
|---|---|---|
| T1 same-origin link | allowed | `NavigationStarted` |
| T2 external link, same window | cancelled | `NavigationStarted` |
| T3 `target=_blank` | no popup, URL captured | `NewWindowRequested` |
| T4 `window.open` | nothing opens, nothing logged | see findings |
| T5 `mailto:` | cancelled | `NavigationStarted` |
| T7 geolocation | nothing logged | no permission hook on macOS |
| I1 email link (base `_blank`) | no popup, URL captured | `NewWindowRequested` |
| I2 email link `target=_self` | cancelled | `NavigationStarted`, with no extra hook |
| I3 email link `target=_top` | no navigation; URL captured as a new-window request | `NewWindowRequested` |
| I4 email meta refresh | never navigates | iframe sandbox |
| I5 email form post | never navigates | iframe sandbox |

Findings:

- **Iframe navigations need no extra wiring on macOS.** `NavigationStarted` covers subframes and honours `Cancel`, as the decompiled adapter showed.
- **`window.open` is not surfaced.** Avalonia's macOS adapter has no `createWebView` handler, so WebKit opens nothing and the shell never sees the URL. That is safe (email frames cannot run script), but it means the web UI must open new windows with `target="_blank"` links, never `window.open`. The Angular app does not use `window.open` today.
- **`target=_top` from the sandboxed email becomes a new-window request** on macOS instead of being dropped silently as on Windows. The shell opens it in the browser, which is the right outcome for a clicked link.

### Linux — out of scope

Decided 2026-09-27: no Linux desktop shell. Linux users run the service (Docker or console) and use the web UI in a browser. The Linux column above is kept for reference only.

## Still to do (plan spike items)

- Self-contained publish + `vpk pack` on Windows and macOS (item 5)
- OS notifications on Windows and macOS (item 7)
