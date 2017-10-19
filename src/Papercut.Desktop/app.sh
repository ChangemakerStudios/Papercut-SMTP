#!/bin/sh
# This script should be executed with administrator previledge

# build
if [ "$1" = "build" ]; then 
	echo "Cleaning up..."

	# cleanup
	rm *.dll
	rm *.pdb
	rm *.dll.config
	rm *.deps.json
    rm *.runtimeconfig.json
	rm -rf Logs
	rm -rf runtimes
    rm -rf \\Incoming


    echo "Building..."
    dotnet publish -o . -c Debug -r osx.10.12-x64
    rm Papercut.Service.deps.json
    rm Papercut.DesktopService.deps.json
fi

# debug
unset DEBUG_PAPERCUT
unset EDGE_DEBUG
if [ "$1" = "debug" ]; then 
    echo "debugging"
    export DEBUG_PAPERCUT=TRUE
	export EDGE_DEBUG=1
fi

unset DEBUG_PAPERCUT_APP
if [ "$1" = "debugapp" ]; then 
    export DEBUG_PAPERCUT_APP=TRUE
fi



# run
echo "Launching the Papercut Desktop Application..."
electron .