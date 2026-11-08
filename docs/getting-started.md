# Getting Started

## Install the Desktop App

=== "Installer"

    Direct download links (always the latest version):

    - **[PapercutSMTP-win-x64-stable-Setup.exe](https://github.com/ChangemakerStudios/Papercut-SMTP/releases/latest/download/PapercutSMTP-win-x64-stable-Setup.exe)** — 64-bit (most common)
    - **[PapercutSMTP-win-x86-stable-Setup.exe](https://github.com/ChangemakerStudios/Papercut-SMTP/releases/latest/download/PapercutSMTP-win-x86-stable-Setup.exe)** — 32-bit
    - **[PapercutSMTP-win-arm64-stable-Setup.exe](https://github.com/ChangemakerStudios/Papercut-SMTP/releases/latest/download/PapercutSMTP-win-arm64-stable-Setup.exe)** — ARM64 (Surface Pro X, etc.)
    - **[PapercutSMTP-osx-arm64-stable-Setup.pkg](https://github.com/ChangemakerStudios/Papercut-SMTP/releases/latest/download/PapercutSMTP-osx-arm64-stable-Setup.pkg)** — macOS, Apple Silicon
    - **[PapercutSMTP-osx-x64-stable-Setup.pkg](https://github.com/ChangemakerStudios/Papercut-SMTP/releases/latest/download/PapercutSMTP-osx-x64-stable-Setup.pkg)** — macOS, Intel

    All releases (portable builds included) are on the [releases page](https://github.com/ChangemakerStudios/Papercut-SMTP/releases).

    Run it and you're done — Papercut keeps itself up to date automatically.

    !!! note "macOS"
        The macOS build isn't signed yet. If macOS blocks the first launch, go to **System Settings → Privacy & Security** and choose **Open Anyway**.

=== "WinGet"

    ```powershell
    winget install ChangemakerStudios.Papercut-SMTP
    ```

    !!! note
        The WinGet package may lag behind the latest release. The direct installer links (see the **Installer** tab) are always current — and the app updates itself automatically after install either way.

=== "Portable"

    Prefer no installer? Download `PapercutSMTP-win-*-stable-Portable.zip` from the [latest release](https://github.com/ChangemakerStudios/Papercut-SMTP/releases/latest), extract, and run `Papercut SMTP.exe`.

=== "Silent / CI"

    The installer supports silent and unattended installation:

    ```powershell
    .\PapercutSMTP-win-x64-stable-Setup.exe --silent --log "install.log"
    ```

    See the [Installation Guide](https://github.com/ChangemakerStudios/Papercut-SMTP/blob/develop/installation/README.md) for all command-line parameters.

### Requirements

- Windows 10 or later, or macOS
- On Windows, the [WebView2 Runtime](https://developer.microsoft.com/en-us/microsoft-edge/webview2) for the Papercut window — preinstalled on all current Windows versions, and the installer adds it if it's missing

## First Run

Start Papercut SMTP. That's the setup — it is now an SMTP server listening on **port `25`** (**`2525`** on macOS), with an icon in the system tray (the menu bar on macOS). Choose **Open Papercut** from its menu to see your inbox.

Choose **View Logs** from the same menu if you want confirmation: you'll see the SMTP server start and its listening address.

## Send Your First Test Email

Point any application at `localhost:25` (`localhost:2525` on macOS) — or try it straight from PowerShell:

```powershell
Send-MailMessage -SmtpServer localhost -Port 25 `
    -From "test@local" -To "anyone@anywhere.example" `
    -Subject "Hello Papercut!" -Body "It works!"
```

The message appears in Papercut instantly, with a tray notification. The `To` address doesn't matter — nothing is ever delivered anywhere.

Next: [configure your application](send-from-your-app.md) to send through Papercut.

## Change the Port or IP

**Options** in the Papercut window lets you change:

- **IP address** — default `Any`, which accepts email from other machines on your network. Pick `127.0.0.1` to accept email from this machine only.
- **Port** — default `25` (`2525` on macOS). Common alternatives: `2525`, `587`.
- **Sort order**, theme, and the [MCP server](mcp.md).

Run at startup and notifications are in the window's **Options** menu. See [Desktop App](desktop.md#options) for where messages are stored.

## Optional Next Steps

- Receive email even when the desktop app is closed → [Service & Web UI](service.md)
- Run Papercut in a container → [Docker](docker.md)
- Test secure email flows → [TLS & Authentication](smtp-tls-auth.md)
