#!/bin/bash

set -e

if [ "$1" != "nobuild" ]; then
    # we need to use this command to 'build' an Electron.NET app. (But actually, we don't need to run it at this time; however, Electron.NET does not provide a switch for prevent running)
    echo 'rebuilding electron app... (To sktip rebuilding, just add 'nobuild' as an argument)'
    dotnet electronize start
fi

# On lanching, we need to quit Papercut app once manually. (The app is launched automatically by Electron.NET start command...)
# Because the our main.js is not applied before we execute following scripts:

cp -f obj/Host/bin/main.js obj/Host/main.js
cp -f obj/Host/bin/package.json obj/Host/package.json
cp -f obj/Host/bin/launch.sh obj/Host/launch.sh
cd obj/Host/
npm install
cd ./node_modules/.bin/

# Run the app after the main.js is replaced
./electron ../../main.js