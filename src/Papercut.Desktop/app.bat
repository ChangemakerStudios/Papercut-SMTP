@echo off


rem build
if "%1" == "build" (
	echo Cleaning up...

	rem cleanup
	del *.dll
	del *.pdb
	del *.dll.config
	del *.deps.json
	del Logs\*.* /F /Q
	rmdir runtimes /S /Q


    echo Building...
    dotnet publish -o .
)

rem debug
SET DEBUG_PAPERCUT=
if "%1" == "debug" (
    SET DEBUG_PAPERCUT=TRUE
)

rem pushd .\Papercut.Service     rem %CD%
rem SET CORECLR_DIR=C:\Program Files\dotnet\shared\Microsoft.NETCore.App\1.1.2
rem popd


rem run
echo Launching the Papercut Desktop Application...
.\node_modules\electron\dist\electron .