

papercutApp.factory('messageRepository', function($http, $q){

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
        var win = require('electron').remote.getCurrentWindow();
        const nativeRepo = win.nativeMessageRepo;

        function wrapPromise(nativeCall, stringifyArgument){
            return function(){
                const task = $q.defer();
                let args = stringifyArgument ? [JSON.stringify(arguments[0])] : [arguments[0]];
                args.push(function(err, result){
                    if(err !== null){
                        task.reject(err);
                        return;
                    }

                    if(result.Status >= 400){
                        task.reject(new Error('Invocation returned ' + result.Status + ' status code.\n' + result.Content));
                        return;
                    }

                    task.resolve( {data:  result.Content ? JSON.parse( result.Content ) : null, status: result.Status } );
                });
                
                nativeCall.apply(null, args);

                return task.promise;
            }
        }


        return {
            list: function(limit, start){
                var list = wrapPromise(nativeRepo.ListAll, true);
                return list({start, limit});
            },
            get: wrapPromise(nativeRepo.GetDetail, false),
            deleteAll: wrapPromise(nativeRepo.DeleteAll, true),
            onNewMessage: wrapPromise(nativeRepo.OnNewMessageArrives, false)
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