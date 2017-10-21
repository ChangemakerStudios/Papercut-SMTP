
const electron = require('electron');
const path = require('path');
const url = require('url');
const fs = require('fs');

module.exports = function createWindow (nativeService, onClosed) {
    registerPapercutProtocol(electron.protocol, nativeService);
    console.log('Creating the window...');

    const mainWindow = new electron.BrowserWindow({ width: 1020, height: 600, minWidth: 1020, minHeight: 450 });
    mainWindow.setTitle('Papercut');
    mainWindow.setMenu(null);
    mainWindow.loadURL(url.format({
      pathname: path.join(__dirname, '../assets/index.html'),
      protocol: 'file:',
      slashes: true
    }));
    mainWindow.nativeAPI = {
        onNewMessage: nativeService.OnNewMessageArrives
    };
  
    if (process.env.DEBUG_PAPERCUT || process.env.DEBUG_PAPERCUT_APP) {
        mainWindow.openDevTools();
    }
    mainWindow.on('closed', function () {
        onClosed();
    });

    var navigateHandler = createNavigateHandler(mainWindow, nativeService);
    mainWindow.webContents.on('will-navigate', navigateHandler);
    mainWindow.webContents.on('new-window', navigateHandler);
    return mainWindow;
  }

  function registerPapercutProtocol(protocol, nativeService){
    protocol.registerBufferProtocol('papercut', (req, cb) => {
        const absolutePath = req.url.replace('://', ':///');
        const uri = url.parse(absolutePath);
        const hasContent = !!(req.uploadData && req.uploadData.length);

        var request = {
            method : req.method,
            path : uri.path,
            content :  hasContent ? req.uploadData[0].bytes : new Buffer(0),
            contentType : hasContent ? "application/json" : null
        };
        
        console.log('requsting resource ' + absolutePath, request);
        nativeService.RequestResource(request, function(err, result){
            if(err || result.error){
                console.warn(err || result.error);
                cb(null);
                return;
            }
            
            cb(result.value.status < 400 ? {mimeType: result.value.contentType, data: result.value.content} : null);
        });
    });
  }

  function createNavigateHandler(browserWindow, nativeService) {
    return function(e, href){
            try {
                if(!isApi(url.parse(href))) {
                    e.preventDefault();
                    electron.shell.openExternal(href);
                    return;
                }else{
                    e.preventDefault();
                    handleApiRequests(href, browserWindow, nativeService);
                    return;
                }
            }
            catch(err){
                e.preventDefault();
                console.log('Could not process url navigation: ' + href + '\n' + err.message);
            }
    }
  }

  function handleApiRequests(u, browserWindow, nativeService){
    const rawMessageCall = matchRawMessage(u);
    const sectionCall = matchMessageSection(u);

    console.log('handling ' + u);
    if(rawMessageCall !== null){
        const msgId = rawMessageCall[1];
        nativeService.RequestResource({ method: 'GET', path: url.parse(u).path, content: null, contentType: null }, function(err, result){
            if(!err && !result.error && result.value.status < 400){
                saveBuffer(browserWindow, result.value.content, 'Save mail message', msgId);
            }
        });
        return;
    }

    if(sectionCall !== null){
        const msgId = sectionCall[1];
        const sectionIndex = sectionCall[2];

        nativeService.RequestResource({ method: 'GET', path: url.parse(u).path, content: null, contentType: null }, function(err, result){
            if(!err && !result.error && result.value.status < 400){
                saveBuffer(browserWindow, result.value.content, 'Save section', msgId + '-' + sectionIndex);
            }
        });
        return;
    }
  }
  
  function isApi(u){
    return u.protocol === 'file:' && /^(\/[A-Z]\:)?\/api\//i.test(u.pathname);
  }

  function matchRawMessage(u){
    return /messages\/([^\/]+)\/raw$/i.exec(u);
  }

  function matchMessageSection(u){
    return /messages\/([^\/]+)\/sections\/([^\/]+)\/?$/i.exec(u);
  }

  function saveBuffer(win, buffer, dialogTitle, defaultFileName){  
    let defaultSavePath = process.env.HOME || process.env.USERPROFILE;
    defaultSavePath = path.join(defaultSavePath, defaultFileName);

    const savePath = electron.dialog.showSaveDialog(win, {title: dialogTitle, defaultPath: defaultSavePath});
    if(!savePath){
        return;
    }
    
    fs.writeFile(savePath, buffer, function(err) {
        if(err) {
            const warnning = 'Error saving file to ' + savePath + '\n' + err.message;
            console.log(warnning + '\n' + err.stack);
            electron.dialog.showErrorBox('Error on saving', warnning);
        }
    }); 
}