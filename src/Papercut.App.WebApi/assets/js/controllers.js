var papercutApp = angular.module('papercutApp', []);


papercutApp.controller('MailCtrl', function ($scope, $http, $sce, $timeout, $interval) {

  $scope.events = {
    eventDone: 0,
    eventFailed: 0,
    eventCount: 0,
    eventsPending: {}
  };


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
      var url = 'api/messages';
      if ($scope.startIndex > 0) {
          url += "?start=" + $scope.startIndex + "&limit=" + $scope.itemsPerPage;
      } else {
          url += "?limit=" + $scope.itemsPerPage;
      }

      $http.get(url).then(function (resp) {
          $scope.messages = resp.data.Messages;
          $scope.totalMessages = resp.data.TotalMessageCount;
          $scope.countMessages = resp.data.Messages.length;
          $scope.startMessages = $scope.startIndex;
          e.done();
      });
  };

  $scope.refresh();
  $interval(function () {
      if ($scope.startIndex == 0) {
          $scope.refresh();
      }
  }, 8000);


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

     $http.delete('api/messages').finally(function () {
       $scope.refresh();
     });
  };

  $scope.selectMessage = function (message) {
      if ($scope.cache[message.Id]) {
          $scope.preview = $scope.cache[message.Id];
      } else {
          $scope.preview = message;
          var e = startEvent("Loading message", message.Id, "glyphicon-download-alt");
          $http.get('api/messages/' + message.Id).then(function (resp) {
              $scope.cache[message.Id] = resp.data;

              resp.data.previewHTML = $sce.trustAsHtml(resp.data.HtmlBody);
              $scope.preview = resp.data;

              var dateHeader = resp.data.Headers.find(function(h){return h.Name === 'Date'});
              $scope.preview.Date = dateHeader === null ? null : dateHeader.Value;
              e.done();
          });
      }
  };

  $scope.formatMessagePlain = function (message) {
      var body = message.TextBody || '';
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
      return !!message.HtmlBody;
  }

  $scope.hasText = function (message) {
      return !!message.TextBody;
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


  function guid() {
      function s4() {
          return Math.floor((1 + Math.random()) * 0x10000)
              .toString(16)
              .substring(1);
      }
      return s4() + s4() + '-' + s4() + '-' + s4() + '-' +
          s4() + '-' + s4() + s4() + s4();
  }
});



papercutApp.directive('targetBlank', function () {
    return {
        link: function (scope, element, attributes) {
            element.on('load', function () {
                var a = element.contents().find('a');
                a.attr('target', '_blank');
            });
        }
    };
});




papercutApp.directive('bodyHtml', ['$sce', '$timeout', function ($sce, $timeout) {
    return {
        link: function (scope, element, attrs) {
            element.attr('src', "about:blank");
            element.on('load', function () {
                var messageId = scope.$eval(attrs.contentLinkMessageId);
                var htmlContent = $sce.getTrustedHtml(scope.$eval(attrs.bodyHtml));
                htmlContent = stripDangerousTags(htmlContent);
                htmlContent = replaceContentLinks(htmlContent, messageId);

                var body = $(element).contents().find('body');
                body.empty().append( htmlContent );

                $timeout(function () {
                    element.css('height', $(body[0].ownerDocument.documentElement).height() + 100);
                }, 50);
            });


            function stripDangerousTags(html) {
                var tagStarts = /\<\s*(script|style|iframe|frameset|link|applet|object)(?=\s|\>)/gi;
                var tagEnds = /\<\s*\/\s*(script|style|iframe|frameset|link|applet|object)\s*\>/gi;
                var links = /((src|href)\s*=["']?\s*)javascript:/gi;

                return html.replace(tagStarts, '<div style="display:none" ')
                           .replace(tagEnds, '</div>')
                           .replace(links, '$1');
            }

            function replaceContentLinks(html, messageId) {
                return html.replace(/cid:([^"^'^\s^;^,^//^/<^/>]+)/gi, 'api/messages/' + messageId + '/contents/$1');
            }
        }
    };
}]);
