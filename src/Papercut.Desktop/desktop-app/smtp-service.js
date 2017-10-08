process.env.EDGE_USE_CORECLR = 1;

// electron-edge is trying to find a CoreCLR runtime, it tends to find a latest version
process.env.CORECLR_DIR = 'C:\\Program Files\\dotnet\\shared\\Microsoft.NETCore.App\\1.1.2';  // this seems does not work
process.env.EDGE_CORECLR_DIR = 'C:\\Program Files\\dotnet\\shared\\Microsoft.NETCore.App\\1.1.2';   // this seems does not work
process.env.CORECLR_VERSION = '1.1.2';   // this works

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