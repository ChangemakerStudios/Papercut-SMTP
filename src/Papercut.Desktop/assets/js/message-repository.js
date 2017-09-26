

papercutApp.factory('messageRepository', function($http){

    function createHttpBasedRepo(){
        function listMessages(limit, skip){
            var url = '/api/messages?limit='+ limit;
            if (limit > 0) {
                url += "&start=" + skip;
            }
      
            return $http.get(url);
        }
    
        function getMessage(id){
            return $http.get('/api/messages/' + id);
        }
    
        function deleteAllMessages(onComplete){
            $http.delete('/api/messages').finally(function () {
                onComplete();
              });
        }
    
        function onNewMessage(){
            // not implemented yet...
        }

        return {
            list: listMessages,
            get: getMessage,
            deleteAll: deleteAllMessages,
            onNewMessage: onNewMessage
        }
    }

    function createNativeRepo(){
        return {
            list: null,
            get: null,
            deleteAll: null,
            onNewMessage: null
        }
    }

    function isElectron(){
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
    
    return isElectron() ? createNativeRepo() : createHttpBasedRepo();
});