var papercutApp = angular.module('papercutApp', [])
    .config( ['$compileProvider', function( $compileProvider ) { $compileProvider.aHrefSanitizationWhitelist(/^\s*(https?|file|papercut):/);}]);


papercutApp.controller('MailCtrl', function ($scope, $sce, $timeout, $interval, messageRepository) {

  $scope.events = {
    eventDone: 0,
    eventFailed: 0,
    eventCount: 0,
    eventsPending: {}
  };


  $scope.requetedMessageList = false;
  $scope.cache = {};
  $scope.itemsPerPage = 50;
  $scope.startIndex = 0;
  $scope.startMessages = 0;
  $scope.countMessages = 0;
  $scope.totalMessages = 0;
  $scope.messages = [];
  $scope.preview = null;

  if(typeof(Storage) !== "undefined") {
      $scope.itemsPerPage = parseInt(localStorage.getItem("itemsPerPage"), 10)
      if(!$scope.itemsPerPage) {
        $scope.itemsPerPage = 50;
        localStorage.setItem("itemsPerPage", 50)
      }
  }




  $scope.getMoment = function (a) {
      return moment.utc(a, 'YYYY-MM-DDTHH:mm:ss.SSSZ').local();
  };

  $scope.backToInbox = function () {
      $scope.preview = null;
  };
  $scope.backToInboxFirst = function () {
      $scope.preview = null;
      $scope.startIndex = 0;
      $scope.startMessages = 0;
      $scope.refresh();
  };

  $scope.refresh = function () {
      var e = startEvent("Loading messages", null, "glyphicon-download");
      $scope.requetedMessageList = true;

      var index = $scope.startIndex;
      messageRepository.list($scope.itemsPerPage, index)
                       .then(function (resp) {
                            $scope.messages = resp.data.messages;
                            $scope.totalMessages = resp.data.totalMessageCount;
                            $scope.countMessages = resp.data.messages.length;
                            $scope.startMessages = index;
                            e.done();
                        });
  };


  $scope.showUpdated = function (i) {
      $scope.itemsPerPage = parseInt(i, 10);
      if (typeof (Storage) !== "undefined") {
          localStorage.setItem("itemsPerPage", $scope.itemsPerPage);
      }

      $scope.startIndex = 0;
      $scope.refresh();
  };

  $scope.showNewer = function () {
      $scope.startIndex -= $scope.itemsPerPage;
      if ($scope.startIndex < 0) {
          $scope.startIndex = 0;
      }
      $scope.refresh();
  };

  $scope.showOlder = function () {
      $scope.startIndex += $scope.itemsPerPage;
      $scope.refresh();
  };

  $scope.deleteAll = function () {
      if(!window.confirm('Are you sure to delete all messages?')){
        return;
      }

     messageRepository.deleteAll(function(){
        $scope.refresh();
     });
  };

  $scope.selectMessage = function (message) {
      if ($scope.cache[message.id]) {
          $scope.preview = $scope.cache[message.id];
      } else {
          $scope.preview = message;
          var e = startEvent("Loading message", message.id, "glyphicon-download-alt");
          messageRepository.get(message.id)
                            .then(function (resp) {
                                $scope.cache[message.id] = resp.data;

                                resp.data.previewHTML = $sce.trustAsHtml(resp.data.htmlBody);
                                $scope.preview = resp.data;

                                var dateHeader = resp.data.headers.find(function(h){return h.name === 'Date'});
                                $scope.preview.date = dateHeader === null ? null : dateHeader.value;
                                e.done();
                            });
      }
  };
  
  $scope.downloadSection = function (msgId, sectionIndex, name) {
      var url = '/api/messages/' + encodeURIComponent(msgId) + '/sections/' + sectionIndex;
      if (!nativeFeatures.isNative()){
          window.open(url,  '_blank');
          return;
      }
      
      nativeFeatures.download(url,  'Save section as...', name || (msgId + '-' + sectionIndex));
  };
  
  $scope.downloadRawMessage = function (msgId) {
      var url = '/api/messages/' + encodeURIComponent(msgId) + '/raw';
      if (!nativeFeatures.isNative()){
          window.open(url,  '_blank');
          return;
      }

      nativeFeatures.download(url,  'Save mail message...', msgId);
  };

  $scope.formatMessagePlain = function (message) {
      var body = message.textBody || '';
      var escaped = $scope.escapeHtml(body);
      var formatted = escaped.replace(/(https?:\/\/)([-[\]A-Za-z0-9._~:/?#@!$()*+,;=%]|&amp;|&#39;)+/g, '<a href="$&" target="_blank">$&</a>');
      return $sce.trustAsHtml(formatted);
  };


  $scope.escapeHtml = function(html) {
    var entityMap = {
      '&': '&amp;',
      '<': '&lt;',
      '>': '&gt;',
      '"': '&quot;',
      "'": '&#39;'
    };
    return html.replace(/[&<>"']/g, function (s) {
      return entityMap[s];
    });
  }

  $scope.hasHTML = function(message) {
      return !!message.htmlBody;
  }

  $scope.hasText = function (message) {
      return !!message.textBody;
  }

  $scope.date = function(timestamp) {
  	return (new Date(timestamp)).toString();
  };

  function startEvent(name, args, glyphicon) {
      var eID = guid();
      //console.log("Starting event '" + name + "' with id '" + eID + "'")
      var e = {
          id: eID,
          name: name,
          started: new Date(),
          complete: false,
          failed: false,
          args: args,
          glyphicon: glyphicon,
          getClass: function () {
              // FIXME bit nasty
              if (this.failed) {
                  return "bg-danger"
              }
              if (this.complete) {
                  return "bg-success"
              }
              return "bg-warning"; // pending
          },
          done: function () {
              var e = this;
              e.complete = true;
              $scope.events.eventDone++;
              if (this.failed) {
                  // console.log("Failed event '" + e.name + "' with id '" + eID + "'")
              } else {
                  // console.log("Completed event '" + e.name + "' with id '" + eID + "'")
                  $timeout(function () {
                      e.remove();
                  }, 10000);
              }
          },
          fail: function () {
              $scope.events.eventFailed++;
              this.failed = true;
              this.done();
          },
          remove: function () {
              // console.log("Deleted event '" + e.name + "' with id '" + eID + "'")
              if (e.failed) {
                  $scope.events.eventFailed--;
              }
              delete $scope.events.eventsPending[eID];
              $scope.events.eventDone--;
              $scope.events.eventCount--;
              return false;
          }
      };
      $scope.events.eventsPending[eID] = e;
      $scope.events.eventCount++;
      return e;
  }

  function setupNotification(scope) {
      if (nativeFeatures.isNative()) {
          connect();
      }else{
          window.Notification && Notification.requestPermission(function (result) {
              if (result === 'granted'){
                  connect();
              }
          });
      }

      function connect() {
          var retryInternval, isConnected;
          var connection = new signalR.HubConnection('/new-messages');
          tryConnect();
          connection.on('new-message-received', onNewMessage);
          connection.onclose(function() {
              isConnected = false;
              console.log('Connection closed. Will retry 5 seconds later...');
              retryInternval = setInterval(tryConnect, 5000);
          });
          
          
          function tryConnect() {
              if (isConnected){
                  cancelRetry();
                  return;
              }
              
              connection.start().then(function () {
                  isConnected = true;
                  cancelRetry();
                  console.log('New message socket connection is established. connectionId: ' + connection.id);  
              });
          }
          
          function cancelRetry() {
              if (retryInternval){
                  clearInterval(retryInternval);
                  retryInternval = null;
              }
          }
      }
      
      function onNewMessage(msg) {
          if (document.hasFocus() && scope.startIndex === 0) {
              scope.refresh();
          } else {
              nativeFeatures.notify(msg.subject, function () {
                  scope.backToInboxFirst();
              });
          }
      }
  }

  function guid() {
      function s4() {
          return Math.floor((1 + Math.random()) * 0x10000)
              .toString(16)
              .substring(1);
      }
      return s4() + s4() + '-' + s4() + '-' + s4() + '-' +
          s4() + '-' + s4() + s4() + s4();
  }


    $scope.refresh();
    $interval(function () {
        if ($scope.startIndex === 0) {
            $scope.refresh();
        }
    }, 8000);
    setupNotification($scope);
});
