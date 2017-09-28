@echo off

if "%1" == "build" (
    echo building
    dotnet publish ..\Papercut.DesktopService -o ..\Papercut.Desktop\ -r win10-x64
)


rem pushd .\Papercut.Service     rem %CD%
rem SET CORECLR_DIR=C:\Program Files\dotnet\shared\Microsoft.NETCore.App\1.1.2
rem popd

.\node_modules\electron\dist\electron .