
const BrowserWindow = require('electron').BrowserWindow;
const path = require('path');
const url = require('url');

module.exports = function createWindow (nativeService, onClosed) {
    console.log('Creating the window...');
  
    const mainWindow = new BrowserWindow({ width: 1020, height: 600, minWidth: 1020, minHeight: 450 });
    mainWindow.setTitle('Papercut');
    mainWindow.setMenu(null);
    mainWindow.loadURL(url.format({
      pathname: path.join(__dirname, '../assets/index.html'),
      protocol: 'file:',
      slashes: true
    }));
    mainWindow.nativeMessageRepo = {
        listAll: nativeService.ListAllMessages,
        deleteAll: nativeService.DeleteAllMessages,
        get: nativeService.GetMessageDetail,
        onNewMessage: nativeService.OnNewMessageArrives
    };
  
    if (process.env.DEBUG_PAPERCUT || process.env.DEBUG_PAPERCUT_APP) {
        mainWindow.openDevTools();
    }
    mainWindow.on('closed', function () {
        onClosed();
    });
    
    function handleRedirect(e, url, frameName, disposition, options) {
      if(url != mainWindow.webContents.getURL()) {
        e.preventDefault();
        require('electron').shell.openExternal(url);
      }
    }
  
    mainWindow.webContents.on('will-navigate', handleRedirect)
    mainWindow.webContents.on('new-window', handleRedirect)
    return mainWindow;
  }