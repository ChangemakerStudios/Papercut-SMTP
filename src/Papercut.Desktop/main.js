
const electron = require('electron');
const app = electron.app;
// global.appGlobals = { electron, app };

const createWindow = require('./desktop-app/main-window')
const launchSmtpService = require('./desktop-app/smtp-service')

let mainWindow;
let nativeService;

app.setName('Papercut');
electron.protocol.registerStandardSchemes(['papercut']);
app.on('ready', () =>{
  launchSmtpService((result) => {
    nativeService = result;
    mainWindow = createWindow(nativeService, () => { mainWindow = null;  });
  });  
});

app.on('activate', ()=> {
  if (mainWindow === null) {
    mainWindow = createWindow(nativeService, () => { mainWindow = null;  });
  }
});

app.on('window-all-closed', () => {
  if (process.platform !== 'darwin') {
    app.quit();
  }
});

app.on('before-quit', () =>{
  try{
    if(nativeService !== null){
      nativeService.StopService(null, true);
      nativeService = null;
    }
  }catch(e){
    console.error('Error stoping the SMTP service:');
    console.log(e);
  }
});