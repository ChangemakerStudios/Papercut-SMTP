# Getting Started

## Install the Desktop App

=== "Installer"

    Download the installer for your architecture from the [latest release](https://github.com/ChangemakerStudios/Papercut-SMTP/releases/latest):

    - **[PapercutSMTP-win-x64-stable-Setup.exe](https://github.com/ChangemakerStudios/Papercut-SMTP/releases/latest)** — 64-bit (most common)
    - **PapercutSMTP-win-x86-stable-Setup.exe** — 32-bit
    - **PapercutSMTP-win-arm64-stable-Setup.exe** — ARM64 (Surface Pro X, etc.)

    Run it and you're done — Papercut keeps itself up to date automatically.

=== "WinGet"

    ```powershell
    winget install ChangemakerStudios.Papercut-SMTP
    ```

=== "Portable"

    Prefer no installer? Download `PapercutSMTP-win-*-stable-Portable.zip` from the [latest release](https://github.com/ChangemakerStudios/Papercut-SMTP/releases/latest), extract, and run `Papercut.exe`.

=== "Silent / CI"

    The installer supports silent and unattended installation:

    ```powershell
    .\PapercutSMTP-win-x64-stable-Setup.exe --silent --log "install.log"
    ```

    See the [Installation Guide](https://github.com/ChangemakerStudios/Papercut-SMTP/blob/develop/installation/README.md) for all command-line parameters.

### Requirements

- Windows 10 or later
- [WebView2 Runtime](https://developer.microsoft.com/en-us/microsoft-edge/webview2) — preinstalled on all current Windows versions; needed for HTML email rendering

## First Run

Start Papercut SMTP. That's the setup — it is now an SMTP server listening on **`127.0.0.1:25`**, and the main window is your inbox.

Check the **Logs** view if you want confirmation: you'll see the SMTP server start and its listening address.

## Send Your First Test Email

Point any application at `localhost:25` — or try it straight from PowerShell:

```powershell
Send-MailMessage -SmtpServer localhost -Port 25 `
    -From "test@local" -To "anyone@anywhere.example" `
    -Subject "Hello Papercut!" -Body "It works!"
```

The message appears in Papercut instantly, with a tray notification. The `To` address doesn't matter — nothing is ever delivered anywhere.

Next: [configure your application](send-from-your-app.md) to send through Papercut.

## Change the Port or IP

**Options** (gear icon) lets you change:

- **IP address** — default `127.0.0.1` (localhost only). Choose `Any` to accept email from other machines on your network.
- **Port** — default `25`. Common alternatives: `2525`, `587`.
- **Message storage location**, startup behavior (run minimized / start with Windows), theme, and notifications.

## Optional Next Steps

- Receive email even when the desktop app is closed → [Service & Web UI](service.md)
- Run Papercut in a container → [Docker](docker.md)
- Test secure email flows → [TLS & Authentication](smtp-tls-auth.md)
