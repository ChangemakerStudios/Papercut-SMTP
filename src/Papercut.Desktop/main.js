process.env.EDGE_USE_CORECLR = 1;

const electron = require('electron');
const edge = require('electron-edge');
const app = electron.app;
const BrowserWindow = electron.BrowserWindow;

const path = require('path');
const url = require('url');

let mainWindow;
let smtpStopFn;

function launchPapercutServices(onComplete){
  const start = edge.func(require('path').join(__dirname, 'Papercut.Service.dll'));
  const stopRet = start(null, function(err, task){
    if(err === null){
      smtpStopFn = task;
    }
  });
}

function createWindow () {
  mainWindow = new BrowserWindow({width: 800, height: 600});

  mainWindow.loadURL(url.format({
    pathname: path.join(__dirname, 'assets/index.html'),
    protocol: 'file:',
    slashes: true
  }));


  mainWindow.on('closed', function () {
    mainWindow = null;
  });
}

app.on('ready', function(){
  launchPapercutServices();
  createWindow();
});

app.on('window-all-closed', function () {
  if (process.platform !== 'darwin') {
    app.quit();
  }
});

app.on('activate', function () {
  if (mainWindow === null) {
    createWindow();
  }
});
