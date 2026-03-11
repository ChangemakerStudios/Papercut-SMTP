## How to Run Papercut.Service

### Option 1: Run as Console Application
Run from the command line by entering:
```
Papercut.Service.exe
```

### Option 2: Install as Windows Service (Recommended)

The service can be installed to run automatically on Windows startup.

#### Using the Installation Scripts:

**For PowerShell users:**
```powershell
.\install-papercut-service.ps1
```

**For Command Prompt users:**
```cmd
install-papercut-service.bat
```

Both scripts will:
- Require administrator privileges (will prompt if needed)
- Validate that Papercut.Service.exe exists
- Install the service with automatic startup configured
- Start the service immediately

#### Uninstalling the Service:

**PowerShell:**
```powershell
.\uninstall-papercut-service.ps1
```

**Command Prompt:**
```cmd
uninstall-papercut-service.bat
```

#### Manual Installation (Advanced):
You can also install the service manually using `sc.exe`:
```cmd
sc.exe create Papercut.Smtp.Service binPath= "C:\path\to\Papercut.Service.exe" DisplayName= "Papercut SMTP Service" start= auto
sc.exe start Papercut.Smtp.Service
```

## How to Configure Papercut.Service

_Papercut.Service_ does not need manual configuration. When the service and the client (_Papercut.exe_) processes are run at the same time, they will automatically synchronize their configurations. For example, when the SMTP settings is modified in the Papercut UI options, the service will automatically update itself and save these changes. Rule changes work the same way.

### Configuration Files

Configuration is managed through a layered system:

1. **`Papercut.Service.Settings.json`** - User-editable settings that persist UI changes
   - Located in the same directory as `Papercut.Service.exe`
   - Contains configuration with comments outlining options
   - Changes made via the Papercut UI are saved here automatically

2. **`appsettings.json`** - Default configuration
   - Provides baseline defaults (SMTP port: 25, IP: Any)
   - Not recommended to edit directly

3. **`appsettings.Production.json`** - Production/Docker overrides
   - Used when `ASPNETCORE_ENVIRONMENT=Production`
   - Docker deployments use non-privileged ports (SMTP: 2525, HTTP: 8080)

**Configuration Priority:** `Papercut.Service.Settings.json` > `appsettings.{Environment}.json` > `appsettings.json`

### Common Configuration Changes

**Change SMTP Port:**
Edit `Papercut.Service.Settings.json` and set:
```json
{
  "Port": "25"
}
```

**Change HTTP Port and Binding Address:**

The web interface binding can be configured using the `Urls` setting in `appsettings.json` or `appsettings.Production.json`:

**Localhost only (default, most secure):**
```json
{
  "Urls": "http://localhost:8080"
}
```

**Bind to all network interfaces (accessible from any IP):**
```json
{
  "Urls": "http://0.0.0.0:8080"
}
```
Alternatively, use wildcard syntax:
```json
{
  "Urls": "http://+:8080"
}
```
or
```json
{
  "Urls": "http://*:8080"
}
```

**Bind to a specific IP address:**
```json
{
  "Urls": "http://192.168.1.100:8080"
}
```

**Multiple bindings (listen on multiple addresses):**
```json
{
  "Urls": "http://localhost:8080;http://192.168.1.100:8080"
}
```

**Security Note:** ⚠️ When binding to `0.0.0.0`, `+`, `*`, or a non-localhost IP address, the web interface becomes accessible from other machines on the network. Ensure proper firewall rules and network security are in place, as Papercut does not include built-in authentication.

**Change SMTP IP Address:**
Edit `Papercut.Service.Settings.json`:
```json
{
  "IP": "127.0.0.1"
}
```

**Note:** Any manual configuration changes require the service to be restarted to take effect.

---

## Option 3: Run in Docker

For complete Docker deployment instructions including examples for Docker Compose, Kubernetes, volume persistence, and troubleshooting, see the **[Docker Hub page](https://hub.docker.com/r/changemakerstudiosus/papercut-smtp)**.

### Quick Docker Start

```bash
# Pull and run with default non-privileged ports (2525 for SMTP, 8080 for HTTP)
docker pull changemakerstudiosus/papercut-smtp:latest
docker run -d -p 37408:8080 -p 2525:2525 changemakerstudiosus/papercut-smtp:latest
```

Access at: **http://localhost:37408** | Send emails to: **localhost:2525**

### Docker Configuration via Environment Variables

```bash
docker run -d \
  -e SmtpServer__Port=2525 \
  -e Urls=http://0.0.0.0:8080 \
  -p 8080:8080 -p 2525:2525 \
  changemakerstudiosus/papercut-smtp:latest
```

**Note:** Docker uses `appsettings.Production.json` which sets non-privileged ports by default (SMTP: 2525, HTTP: 8080).

### Volume Permissions

The container runs as a non-root user by default. If you get permission errors when mounting volumes (e.g., for `/app/Incoming` or `/app/Logs`), you can run as root:

```bash
docker run -d --user 0:0 \
  -p 8080:8080 -p 2525:2525 \
  -v papercut-data:/app/Incoming \
  changemakerstudiosus/papercut-smtp:latest
```

Or in Docker Compose, add `user: "0:0"` to the service:

```yaml
services:
  papercut-smtp:
    image: changemakerstudiosus/papercut-smtp:latest
    user: "0:0"
    volumes:
      - papercut-data:/app/Incoming
```

Alternatively, ensure host directories are writable by UID 1654 (the default non-root user).
