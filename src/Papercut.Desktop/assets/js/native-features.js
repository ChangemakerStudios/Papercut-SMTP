window.nativeFeatures = {
    isNative : function() {
        // check if in electron: https://github.com/cheton/is-electron/blob/master/index.js

        if (typeof window !== 'undefined' && typeof window.process === 'object' && window.process.type === 'renderer') {
            return true;
        }
    
        // Main process
        if (typeof process !== 'undefined' && typeof process.versions === 'object' && !!process.versions.electron) {
            return true;
        }

        return false;
    }
};


// Native setup
(function(){
    if(!window.nativeFeatures.isNative()){
        return;
    }

    window.nativeWindow = require('electron').remote.getCurrentWindow();
    window.webContents = window.nativeWindow.webContents;
})();


  