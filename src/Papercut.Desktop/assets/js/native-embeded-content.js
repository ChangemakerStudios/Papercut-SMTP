papercutApp.service('nativeEmbededContent', [function () {
    return {
        replaceAllImages: function(el){
            if(!window.nativeFeatures.isNative()){
                return;
            }

            var fetchBase64 = window.nativeWindow.nativeMessageRepo.getBase64Content;
            var contentPattern = /\/messages\/([^\/]+)\/contents\/([^\/]+)$/i;
            $.makeArray(el.find('img'))
             .map(function(i){ return {element: i, match: contentPattern.exec(i.src) }; })
             .filter(function(img){ return !!img.match })
             .forEach(function(img){
                var msgId = img.match[1], contentId = img.match[2];

                fetchBase64(JSON.stringify([msgId, contentId]), function(err, base64){
                    if(!err){
                        img.element.src = base64;
                    }
                });
             });
        }
    }
}]);