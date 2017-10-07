process.env.EDGE_USE_CORECLR = 1;

// electron-edge is trying to find a CoreCLR runtime, it tends to find a latest version
process.env.CORECLR_DIR = 'C:\\Program Files\\dotnet\\shared\\Microsoft.NETCore.App\\1.1.2';  // this seems does not work
process.env.EDGE_CORECLR_DIR = 'C:\\Program Files\\dotnet\\shared\\Microsoft.NETCore.App\\1.1.2';   // this seems does not work
process.env.CORECLR_VERSION = '1.1.2';   // this works

const electron = require('electron');
const edge = require('electron-edge');
const app = electron.app;
const BrowserWindow = electron.BrowserWindow;

const path = require('path');
const url = require('url');

let mainWindow;
let nativeService;

function launchPapercutServices(onComplete){
  var assembly = require('path').join(__dirname, 'Papercut.Desktop.dll');
  const start = edge.func(assembly);
   start(null, function(err, result){
    if(err !== null){
      console.error(err);
      return;
    }
    onComplete(result);
   });
}

function createWindow () {
  console.log('creating the window...');

  mainWindow = new BrowserWindow({ width: 1000, height: 600, minWidth: 1000, minHeight: 450 });
  mainWindow.setTitle('Papercut');
  mainWindow.setMenu(null);
  mainWindow.loadURL(url.format({
    pathname: path.join(__dirname, 'assets/index.html'),
    protocol: 'file:',
    slashes: true
  }));
  mainWindow.nativeMessageRepo = {
      listAll: nativeService.ListAllMessages,
      deleteAll: nativeService.DeleteAllMessages,
      get: nativeService.GetMessageDetail,
      onNewMessage: nativeService.OnNewMessageArrives
  };

  if (process.env.DEBUG_PAPERCUT) {
      mainWindow.openDevTools();
  }
  mainWindow.on('closed', function () {
    mainWindow = null;
  });
}

app.on('ready', function(){
  launchPapercutServices(function(result){
    nativeService = result;
    createWindow();
  });  
});

app.on('window-all-closed', function () {
  if (process.platform !== 'darwin') {
    try{
      nativeService.StopService(null, true);
      nativeService = null;
    }catch(e){
      console.error('Error stoping the smtp service:');
      console.log(e);
    }

    app.quit();
  }
});

app.on('activate', function () {
  if (mainWindow === null) {
    createWindow();
  }
});
