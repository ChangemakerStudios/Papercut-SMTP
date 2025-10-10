<#
.SYNOPSIS
    Papercut SMTP Installer Helper Script

.DESCRIPTION
    This script provides a PowerShell-friendly way to install Papercut SMTP with
    proper help documentation. It wraps the VeloPack Setup.exe installer.

.PARAMETER Silent
    Run installation silently without prompts

.PARAMETER Verbose
    Enable verbose logging output

.PARAMETER Log
    Specify a log file path for installation logs

.PARAMETER InstallTo
    Specify the installation directory

.PARAMETER Help
    Display this help information

.EXAMPLE
    .\Install-PapercutSMTP.ps1
    Run the installer interactively

.EXAMPLE
    .\Install-PapercutSMTP.ps1 -Silent
    Run silent installation

.EXAMPLE
    .\Install-PapercutSMTP.ps1 -InstallTo "C:\Program Files\PapercutSMTP"
    Install to a specific directory

.EXAMPLE
    .\Install-PapercutSMTP.ps1 -Silent -Log "install.log"
    Silent installation with logging

.NOTES
    For more information, visit: https://github.com/ChangemakerStudios/Papercut-SMTP
#>

[CmdletBinding()]
param(
    [Parameter(HelpMessage="Run installation silently without prompts")]
    [Alias("s")]
    [switch]$Silent,

    [Parameter(HelpMessage="Enable verbose logging output")]
    [Alias("v")]
    [switch]$VerboseLogging,

    [Parameter(HelpMessage="Specify a log file path for installation logs")]
    [Alias("l")]
    [string]$Log,

    [Parameter(HelpMessage="Specify the installation directory")]
    [Alias("t")]
    [string]$InstallTo,

    [Parameter(HelpMessage="Display help information")]
    [Alias("h", "?")]
    [switch]$Help
)

function Show-InstallerHelp {
    Write-Host @"

Papercut SMTP Installer
========================

A development-focused SMTP server with email viewer for testing email functionality.

USAGE:
    .\Install-PapercutSMTP.ps1 [OPTIONS]

OPTIONS:
    -Silent, -s              Run installation silently without prompts
    -VerboseLogging, -v      Enable verbose logging output
    -Log, -l <FILE>          Specify a log file path for installation logs
    -InstallTo, -t <DIR>     Specify the installation directory
    -Help, -h, -?            Display this help information

EXAMPLES:
    Interactive installation:
        .\Install-PapercutSMTP.ps1

    Silent installation:
        .\Install-PapercutSMTP.ps1 -Silent

    Install to specific directory:
        .\Install-PapercutSMTP.ps1 -InstallTo "C:\Apps\PapercutSMTP"

    Silent install with logging:
        .\Install-PapercutSMTP.ps1 -Silent -Log "install.log"

DIRECT SETUP.EXE USAGE:
    You can also run the Setup.exe directly with these parameters:

    .\PapercutSMTP-win-x64-stable-Setup.exe [OPTIONS]

    Note: Setup.exe is a GUI application. To see help output in PowerShell:
    .\PapercutSMTP-win-x64-stable-Setup.exe --help --log help.txt
    type help.txt

For more information, visit:
    https://github.com/ChangemakerStudios/Papercut-SMTP

"@ -ForegroundColor Cyan
}

# Show help if requested
if ($Help) {
    Show-InstallerHelp
    exit 0
}

# Find the Setup.exe in the same directory as this script
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$SetupExe = Get-ChildItem -Path $ScriptDir -Filter "*Setup.exe" | Select-Object -First 1

if (-not $SetupExe) {
    Write-Error "Could not find Setup.exe in directory: $ScriptDir"
    Write-Host "Please ensure this script is in the same directory as the PapercutSMTP Setup.exe" -ForegroundColor Yellow
    exit 1
}

# Build arguments for Setup.exe
$Arguments = @()

if ($Silent) {
    $Arguments += "--silent"
}

if ($VerboseLogging) {
    $Arguments += "--verbose"
}

if ($Log) {
    $Arguments += "--log"
    $Arguments += $Log
}

if ($InstallTo) {
    $Arguments += "--installto"
    $Arguments += $InstallTo
}

# Run the installer
Write-Host "Starting Papercut SMTP installation..." -ForegroundColor Green
Write-Host "Installer: $($SetupExe.FullName)" -ForegroundColor Gray

if ($Arguments.Count -gt 0) {
    Write-Host "Arguments: $($Arguments -join ' ')" -ForegroundColor Gray
}

try {
    if ($Arguments.Count -gt 0) {
        Start-Process -FilePath $SetupExe.FullName -ArgumentList $Arguments -Wait
    } else {
        Start-Process -FilePath $SetupExe.FullName -Wait
    }

    Write-Host "`nInstallation completed." -ForegroundColor Green
} catch {
    Write-Error "Installation failed: $_"
    exit 1
}
