
const { app } = require('electron');
const path = require('path');
const portfinder = require('detect-port');
const isWin32 = require("os").platform() === "win32";

let io, browserWindows, ipc, loadURL;
let appApi, menu, dialog, notification, tray, webContents;
let globalShortcut, shell, screen, clipboard;

let serviceConfig = require("./bin/Papercut.Service.json");
let useRoot = !isWin32 && parseInt(serviceConfig.Port) < 1024;
let startProcess;

if (useRoot){
    startProcess = function (binFilePath, parameters) {
        const sudo = require('sudo-prompt');
        const sudoOptions = {
            name: 'Papercut'
        };
        
        const bin = binFilePath + ' ' + (parameters || []).join(' ');
        sudo.exec(bin, sudoOptions,
            function(error, stdout, stderr) {
                if (error) throw error;
                console.log('stdout: ' + stdout);
                
                // BUG: Papercut.Desktop is a long running application whose output cannot be captured by 'sudo-prompt' (see https://github.com/jorangreef/sudo-prompt/issues/50) 
                // TODO: To redirect output into a file in debug mode; To wait a period of time in Papercut.Desktop application for the debugger to attach.
            }
        );
    };
}else{
    startProcess = function (binFilePath, parameters) {
        var process = require('child_process').spawn(binFilePath, parameters);
        process.stdout.on('data', (data) => {
            console.log(`stdout: ${data.toString()}`);
        });
    };
}


app.on('ready', () => {
    portfinder(8000, (error, port) => {
        startSocketApiBridge(port);
    });
});


function startSocketApiBridge(port) {
    io = require('socket.io')(port);
    startAspCoreBackend(port);

    io.on('connection', (socket) => {
        console.log('ASP.NET Core Application connected...');

        appApi = require('./api/app')(socket, app);
        browserWindows = require('./api/browserWindows')(socket);
        ipc = require('./api/ipc')(socket);
        menu = require('./api/menu')(socket);
        dialog = require('./api/dialog')(socket);
        notification = require('./api/notification')(socket);
        tray = require('./api/tray')(socket);
        webContents = require('./api/webContents')(socket);
        globalShortcut = require('./api/globalShortcut')(socket);
        shell = require('./api/shell')(socket);
        screen = require('./api/screen')(socket);
        clipboard = require('./api/clipboard')(socket);
    });
}

function startAspCoreBackend(electronPort) {
    portfinder(8000, (error, electronWebPort) => {
        loadURL = `http://localhost:${electronWebPort}`
        const parameters = [`/electronPort=${electronPort}`, `/electronWebPort=${electronWebPort}`];

        const manifestFile = require("./bin/electron.manifest.json");
        let binaryFile = manifestFile.executable;

        if (isWin32) {
            binaryFile = binaryFile + '.exe';
        }

        const binFilePath = path.join(__dirname, 'bin', binaryFile);
        startProcess(binFilePath, parameters);
    });
}

// Quit when all windows are closed.
app.on('window-all-closed', () => {
    app.quit();
});

//app.on('activate', () => {
// On macOS it's common to re-create a window in the app when the
// dock icon is clicked and there are no other windows open.
//    if (win === null) {
//        createWindow();
//    }
//});