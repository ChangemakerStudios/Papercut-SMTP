
process.env["PAPERCUT_BOOTSTRAPER"] = "Customized";
process.debugPapercut = !!process.env["DEBUG_PAPAERCUT"];
const { app, Menu, Tray } = require('electron');
const path = require('path');
const portfinder = require('detect-port');
const osPlatform = require("os").platform();
const isWin32 = osPlatform === "win32";

let io, browserWindows, ipc, loadURL;
let appApi, menu, dialog, notification, tray, webContents;
let globalShortcut, shell, screen, clipboard;

let serviceConfig = require("./bin/Papercut.Service.json");
let useRoot = !isWin32 && parseInt(serviceConfig.Port) < 1024;
let startProcess, backendProcess;
let activeWindow, trayIcon, trayNotify;


if (useRoot){
    startProcess = function (binFilePath, parameters) {
        const outoutFile = setupOutputRedirection();
        const sudo = require('sudo-prompt');
        const sudoOptions = {
            name: 'Papercut'
        };

        const bin = binFilePath + ' ' + (parameters || []).join(' ');
        let proxy = path.join(__dirname, 'launch.sh');
        proxy += ' "' + bin.replace('"', '\\"') + '" ' + outoutFile;
        sudo.exec(proxy, sudoOptions,
            function(error) {
                if (error) throw error;
            }
        );
    };
}else{
    startProcess = function (binFilePath, parameters) {
        const outoutFile = setupOutputRedirection();
        const output = require('fs').openSync(outoutFile, 'a');
        backendProcess = require('child_process').spawn(binFilePath, parameters, {detached: false, stdio: [ 'ignore', output, output ]});
    };
}


app.on('will-quit', () => {
    console.log('All windows are closed, application quiting...');
    exit();
});

app.on('window-all-closed', () => {
    console.log('All windows are closed, application quiting...');
    exit();
});

app.on('web-contents-created', (e, webContents)=>{
    const windowUrl = webContents.getURL();
    function handleRedirect(e, url) {
        if(normalizeUrl(url) !== normalizeUrl(windowUrl)) {
            e.preventDefault();
            require('electron').shell.openExternal(url);
        }


        function normalizeUrl(url) {
            url = url.toLowerCase();

            var hash = url.lastIndexOf('#');
            if (hash > -1){
                url = url.substr(0, hash);
            }

            return url;
        }
    }

    webContents.on('will-navigate', handleRedirect);
    webContents.on('new-window', handleRedirect);
    webContents.on('tray-notify', function (notification) {
        trayNotify && trayNotify(notification);
    })
});

app.on('browser-window-created', (e, window) => {
    activeWindow = window;
});

app.on('ready', () => {
    portfinder(8000, (error, port) => {
       startSocketApiBridge(port);
    });
    
    if (osPlatform === 'darwin') {
        const localShortcut = require('electron-localshortcut');
        localShortcut.register('Command+H', function () {
            app.hide();
        });
    }
    
    if (isWin32 && 10 > parseInt(require("os").release())){
        setupTrayIcon();
    }
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
    portfinder(37408, (error, electronWebPort) => {
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

function setupOutputRedirection() {
    const fs = require('fs');
    const tmp = require('tmp');
    const papercutTmpDir = path.join(tmp.tmpdir, 'Papercut');

    let debugPapercut = process.debugPapercut;
    if (!debugPapercut) {
        tmp.setGracefulCleanup();
    }

    if (!fs.existsSync(papercutTmpDir)){
        fs.mkdirSync(papercutTmpDir)
    }

    let tempFile = tmp.fileSync(
        {
            prefix: 'Output-',
            postfix: '.log',
            discardDescriptor: true,
            dir: papercutTmpDir,
            keep: debugPapercut
        });

    console.log('Output from Papercut backend service will be written at "' + 
                tempFile.name + '".\n' + 
                (debugPapercut ? 'This file will be kept after Papercut exits since we are in debug mode.' : 'This file will be removed after Papercut exits.'));
    return tempFile.name;
}

function exit() {
    if (trayIcon){
        trayIcon.destroy();
        trayIcon = null;
    }
    
    try {
        if (backendProcess != null) {
            backendProcess.kill('SIGQUIT');
        }
    }catch (err){}
    
    setTimeout(function () {
        process.exit(0);
    }, 100);
    app.exit(0);
}

function setupTrayIcon() {
    const iconPath = __dirname + '/bin/icons/Papercut-icon.png';
    
    trayIcon = new Tray(iconPath);
    const contextMenu = Menu.buildFromTemplate([
        {label: 'Quit Papercut', type: 'normal'}
    ]);

    contextMenu.items[0].click = function () {
        app.quit();
    };

    trayIcon.setToolTip('Papercut');
    trayIcon.setContextMenu(contextMenu);


    trayIcon.on('click', activePapercut);
    trayIcon.on('balloon-click', activePapercut);
    
    trayNotify = function (notification) {
        if (!trayIcon){ return; }
        
        var options = Object.assign({ icon: iconPath }, notification);
        trayIcon.displayBalloon(options);
    }
}

function activePapercut() {
    if (activeWindow){
        activeWindow.show();
        activeWindow.focus();
    }
}