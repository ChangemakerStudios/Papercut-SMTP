@echo off
REM Papercut SMTP Service - Uninstallation Script
REM This batch file wrapper runs the PowerShell uninstallation script

echo.
echo ========================================
echo Papercut SMTP Service - Uninstallation
echo ========================================
echo.

REM Get the directory where this script is located
set "SCRIPT_DIR=%~dp0"

REM Check if PowerShell script exists
if not exist "%SCRIPT_DIR%uninstall-papercut-service.ps1" (
    echo ERROR: Could not find uninstall-papercut-service.ps1
    echo Please ensure the PowerShell script is in the same directory as this batch file.
    echo.
    pause
    exit /b 1
)

REM Run the PowerShell script with elevated privileges
echo Running uninstallation script...
echo.
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%SCRIPT_DIR%uninstall-papercut-service.ps1"

REM Check if PowerShell execution failed
if errorlevel 1 (
    echo.
    echo ERROR: Uninstallation script failed to execute.
    echo Please ensure you have PowerShell installed and try running as Administrator.
    echo.
    pause
    exit /b 1
)

exit /b 0
