process.env.EDGE_USE_CORECLR = 1;
if(process.platform === 'darwin'){
  process.env.EDGE_NATIVE = '/Users/jjchen/Projects/Papercut/src/Papercut.Desktop/node_modules/electron-edge/lib/native/edge_coreclr.node';
  process.env.EDGE_APP_ROOT = require('path').join(__dirname, '../');
}else if(process.platform === 'win32'){
  // electron-edge is trying to find a CoreCLR runtime, it tends to find a latest version
  process.env.CORECLR_DIR = 'C:\\Program Files\\dotnet\\shared\\Microsoft.NETCore.App\\1.1.2';  // this seems does not work
  process.env.EDGE_CORECLR_DIR = 'C:\\Program Files\\dotnet\\shared\\Microsoft.NETCore.App\\1.1.2';   // this seems does not work
  process.env.CORECLR_VERSION = '1.1.2';   // this works
}

const edge = require('electron-edge');


module.exports = function launchSmtpService(onComplete){
  const assembly = require('path').join(__dirname, '..', 'Papercut.Desktop.dll');
  const start = edge.func(assembly);
    start(null, function(err, result){
      if(err !== null){
        console.error(err);
        return;
      }
      
      onComplete(result);
    });
}