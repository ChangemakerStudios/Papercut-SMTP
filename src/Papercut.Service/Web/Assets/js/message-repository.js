

papercutApp.factory('messageRepository', function($http, $q){
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
    };
});