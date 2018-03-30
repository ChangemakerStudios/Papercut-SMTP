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
    },
    download: function (url, dialogTitle, defaultFileName){
        const remote = require('electron').remote;
        const dialog = remote.require('electron').dialog,
            fs = remote.require('fs'),
            http = remote.require('http'),
            path = remote.require('path');

        let defaultSavePath = process.env.HOME || process.env.USERPROFILE;
        defaultSavePath = path.join(defaultSavePath, defaultFileName);
        const savePath = dialog.showSaveDialog(window.nativeWindow, {title: dialogTitle, defaultPath: defaultSavePath});
        if(!savePath){
            return;
        }

        if (!/^https?\:/i.test(url)){
            url = location.protocol + '//' + location.host + url;
        }
        
        try {
            var file = fs.createWriteStream(savePath);
            http.get(url, function (response) {
                response.pipe(file);
            });
        }catch (err){
            const warnning = 'Error saving file to ' + savePath + '\n' + err.message;
            console.log(warnning + '\n' + err.stack);
            dialog.showErrorBox('Error on saving', warnning);
        }
    },
    notify: function (message, onclick) {
        if (!this.isNative() && !Notification.permission){
            return;
        }

        var msgPrompt = {
           body: message,
           icon: '/images/Papercut-icon.png'
        };
        
        if (this.isNative()){
            // As a native app, the notification will defaultly contain the application's logo.
            delete msgPrompt.icon;
        }
        
        var notification = new Notification('New Message Receivied', msgPrompt);
        notification.onclick = function () {
            parent.focus();
            window.focus();
            
            setTimeout(onclick, 0);
            notification.close();
        };
        notification.show();
    }
};

(function(){
    if(!window.nativeFeatures.isNative()){
        return;
    }

    window.nativeWindow = require('electron').remote.getCurrentWindow();
    window.webContents = window.nativeWindow.webContents;
})();


  