@echo off



rem we need to use this command to 'build' an Electron.NET app. (But actually, we don't need to run it at this time; however, Electron.NET does not provide a switch for prevent running)
if NOT "%1"=="nobuild" (
    echo rebuilding electron app... (To sktip rebuilding, just add 'nobuild' as an argument ^^^)
    electronize start
)


rem On lanching, we need to quit Papercut app once manually. (The app is launched automatically by Electron.NET start command...)
rem Because the our main.js is not applied before we execute following scripts:

copy /Y obj\Host\bin\main.js obj\Host\main.js
copy /Y obj\Host\bin\package.json obj\Host\package.json
copy /Y obj\Host\bin\launch.sh obj\Host\launch.sh

cd obj\Host\
call npm install


rem Run the app after the main.js is replaced
echo launching the Papercut desktop app...
.\node_modules\electron\dist\electron.exe ".\main.js"