@echo off
setlocal enabledelayedexpansion

if "%~1"=="" (
    echo Version number is required.
    echo Usage: build-vpk.bat [version]
    exit /b 1
)

set "version=%~1"

rm .\publish\*.* -r

dotnet publish -c Release -r win-x64 -o .\publish\x64
dotnet publish -c Release -r win-x86 -o .\publish\x86

vpk pack -u PapercutSMTPx64 --packTitle "Papercut SMTP" -r win7-x64 --icon App.ico -v %version% -p .\publish\x64 -o .\releases -e Papercut.exe --framework net8.0-x64-desktop,webview2
vpk pack -u PapercutSMTPx86 --packTitle "Papercut SMTP" -r win7-x86 --icon App.ico -v %version% -p .\publish\x86 -o .\releases -e Papercut.exe --framework net8.0-x64-desktop,webview2