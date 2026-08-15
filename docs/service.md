# Service & Web UI

The **Papercut SMTP Service** is an optional background component that receives email even when the desktop app isn't running — as a Windows Service or a [Docker container](docker.md) — and includes a browser-based UI for viewing messages.

!!! note "The service is optional"
    For normal desktop use, the Papercut app alone is a complete SMTP server and viewer. Install the service for always-on setups, shared dev servers, or headless environments.

## Web UI

The service serves a web interface at **http://localhost:8080** by default:

![Papercut SMTP Web UI - Message List](https://raw.githubusercontent.com/ChangemakerStudios/Papercut-SMTP/develop/graphics/PapercutWebUI-V7-1.png)

![Papercut SMTP Web UI - Message Detail](https://raw.githubusercontent.com/ChangemakerStudios/Papercut-SMTP/develop/graphics/PapercutWebUI-V7-2.png)

## Install as a Windows Service

1. Download `Papercut.Smtp.Service.*.zip` (x64, x86, or ARM64) from the [latest release](https://github.com/ChangemakerStudios/Papercut-SMTP/releases/latest)
2. Extract it anywhere
3. Run the install script **as administrator**:
   - `install-papercut-service.bat` (double-click), or
   - `.\install-papercut-service.ps1` (PowerShell)

The service installs with automatic startup and starts immediately. To uninstall, run `uninstall-papercut-service.bat` / `.ps1`.

You can also run it as a plain console app — just execute `Papercut.Service.exe`.

## Configuration

The service needs no manual configuration: when the desktop app and service run on the same machine, they **synchronize settings automatically** — change the SMTP port in the desktop Options and the service follows. Rules sync the same way.

For manual configuration, the service uses layered files (highest priority first):

| File | Purpose |
|------|---------|
| `Papercut.Service.Settings.json` | User settings — UI changes are saved here (SMTP `IP`, `Port`, etc.) |
| `rules.json` | Persisted rules |
| `appsettings.Production.json` | Docker/production overrides (non-privileged ports) |
| `appsettings.json` | Baseline defaults (SMTP port 25, IP Any) |

Restart the service after manual edits.

### Common changes

**SMTP port** — `Papercut.Service.Settings.json`:

```json
{ "Port": "2525" }
```

**Web UI binding** — `appsettings.json` `Urls` setting:

```json
{ "Urls": "http://localhost:8080" }
```

Use `http://0.0.0.0:8080` to listen on all interfaces — but read the warning below.

**Path prefix** (reverse proxy / ingress) — serve the web UI under e.g. `/webmail`:

```json
{ "HttpPathPrefix": "/webmail" }
```

!!! warning "Network exposure"
    When binding to `0.0.0.0`, `+`, `*`, or a LAN IP, the web UI is reachable from other machines — and Papercut has **no built-in authentication**. Use firewall rules or a reverse proxy with auth in front of it.

## API

The web UI is backed by a small HTTP API (`/api/messages`, etc.) you can script against — handy for asserting "an email was sent" in end-to-end tests. Explore the endpoints via your browser's dev tools on the web UI.
