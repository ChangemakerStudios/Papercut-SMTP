#!/bin/bash

set -e

# dotnet electronize start
cp -f obj/Host/bin/main.js obj/Host/main.js
cp -f obj/Host/bin/package.json obj/Host/package.json
cp -f obj/Host/bin/launch.sh obj/Host/launch.sh
cd obj/Host/
npm install
cd ./node_modules/.bin/
./electron ../../main.js