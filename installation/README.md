# Papercut SMTP - Installation Guide

## Quick Start

### Interactive Installation
Simply double-click the `Setup.exe` file to start the interactive installer.

### Command-Line Installation

The Setup.exe supports command-line parameters for automated/silent installations.

## Installation Parameters

| Parameter | Short | Description |
|-----------|-------|-------------|
| `--silent` | `-s` | Run installation silently without prompts |
| `--verbose` | `-v` | Enable verbose logging output |
| `--log <file>` | `-l` | Write installation logs to specified file |
| `--installto <dir>` | `-t` | Specify custom installation directory |
| `--help` | `-h` | Display help information |

## Examples

### Silent Installation
```powershell
.\PapercutSMTP-win-x64-stable-Setup.exe --silent
```

### Install to Custom Directory
```powershell
.\PapercutSMTP-win-x64-stable-Setup.exe --installto "C:\Apps\PapercutSMTP"
```

### Silent Install with Logging
```powershell
.\PapercutSMTP-win-x64-stable-Setup.exe --silent --log "install.log"
```

### View Help in PowerShell
Since Setup.exe is a GUI application, the help output won't display directly in PowerShell/cmd.
Use the `--log` parameter to save help output to a file:

```powershell
.\PapercutSMTP-win-x64-stable-Setup.exe --help --log help.txt
type help.txt
```

Or use the included PowerShell helper script:
```powershell
.\Install-PapercutSMTP.ps1 --help
```

## Using the PowerShell Helper Script

A `Install-PapercutSMTP.ps1` script is included for easier installation from PowerShell:

```powershell
# Show help
.\Install-PapercutSMTP.ps1 -Help

# Interactive installation
.\Install-PapercutSMTP.ps1

# Silent installation
.\Install-PapercutSMTP.ps1 -Silent

# Custom directory
.\Install-PapercutSMTP.ps1 -InstallTo "C:\Apps\PapercutSMTP"

# Silent with logging
.\Install-PapercutSMTP.ps1 -Silent -Log "install.log"
```

## Unattended Installation (CI/CD)

For automated deployment scenarios:

```powershell
# Download and install silently
Invoke-WebRequest -Uri "https://github.com/ChangemakerStudios/Papercut-SMTP/releases/latest/download/PapercutSMTP-win-x64-stable-Setup.exe" -OutFile "Setup.exe"
.\Setup.exe --silent --log "install.log"

# Verify installation
if ($LASTEXITCODE -eq 0) {
    Write-Host "Installation successful"
    type install.log
} else {
    Write-Error "Installation failed with exit code $LASTEXITCODE"
    type install.log
    exit $LASTEXITCODE
}
```

## Troubleshooting

### Help doesn't show in console
This is expected behavior. Setup.exe is a GUI application that doesn't attach to the console.

**Solutions:**
1. Use `--log` parameter to save output to a file
2. Use the `Install-PapercutSMTP.ps1` PowerShell script
3. Run from Git Bash or WSL where console attachment works differently

### Installation fails silently
Enable logging to diagnose issues:
```powershell
.\Setup.exe --silent --verbose --log "debug.log"
```

## More Information

- **GitHub**: https://github.com/ChangemakerStudios/Papercut-SMTP
- **Documentation**: https://github.com/ChangemakerStudios/Papercut-SMTP/wiki
- **Issues**: https://github.com/ChangemakerStudios/Papercut-SMTP/issues

## System Requirements

- Windows 7 SP1 or later
- .NET 8.0 Runtime (installed automatically if needed)
- WebView2 Runtime (installed automatically if needed)
