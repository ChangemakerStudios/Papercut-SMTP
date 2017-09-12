var papercutApp = angular.module('papercutApp', []);


papercutApp.controller('MailCtrl', function ($scope, $http, $sce, $timeout, $interval) {
  $scope.host = apiHost;

  $scope.cache = {};
  $scope.previewAllHeaders = false;

  $scope.eventsPending = {};
  $scope.eventCount = 0;
  $scope.eventDone = 0;
  $scope.eventFailed = 0;

  $scope.hasEventSource = false;
  $scope.source = null;

  $scope.itemsPerPage = 50
  $scope.startIndex = 0

  if(typeof(Storage) !== "undefined") {
      $scope.itemsPerPage = parseInt(localStorage.getItem("itemsPerPage"), 10)
      if(!$scope.itemsPerPage) {
        $scope.itemsPerPage = 50;
        localStorage.setItem("itemsPerPage", 50)
      }
  }

  $scope.startMessages = 0
  $scope.countMessages = 0
  $scope.totalMessages = 0


  $scope.smtpmech = "NONE"
  $scope.selectedOutgoingSMTP = ""
  $scope.saveSMTPServer = false;


  $scope.getMoment = function (a) {
      return moment.utc(a, 'YYYY-MM-DDTHH:mm:ss.SSSZ').local();
  }

  $scope.backToInbox = function() {
    $scope.preview = null;
  }
  $scope.backToInboxFirst = function() {
    $scope.preview = null;
    $scope.startIndex = 0;
    $scope.startMessages = 0;
    $scope.refresh();
  }

  $scope.tryDecodeMime = function(str) {
    return unescapeFromMime(str)
  }

  $scope.resizePreview = function() {
    $('.tab-content').height($(window).innerHeight() - $('.tab-content').offset().top);
    $('.tab-content .tab-pane').height($(window).innerHeight() - $('.tab-content').offset().top);
  }

  $scope.startEvent = function(name, args, glyphicon) {
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
      getClass: function() {
        // FIXME bit nasty
        if(this.failed) {
          return "bg-danger"
        }
        if(this.complete) {
          return "bg-success"
        }
        return "bg-warning"; // pending
      },
      done: function() {
        //delete $scope.eventsPending[eID]
        var e = this;
        e.complete = true;
        $scope.eventDone++;
        if(this.failed) {
          // console.log("Failed event '" + e.name + "' with id '" + eID + "'")
        } else {
          // console.log("Completed event '" + e.name + "' with id '" + eID + "'")
          $timeout(function() {
            e.remove();
          }, 10000);
        }
      },
      fail: function() {
        $scope.eventFailed++;
        this.failed = true;
        this.done();
      },
      remove: function() {
        // console.log("Deleted event '" + e.name + "' with id '" + eID + "'")
        if(e.failed) {
          $scope.eventFailed--;
        }
        delete $scope.eventsPending[eID];
        $scope.eventDone--;
        $scope.eventCount--;
        return false;
      }
    };
    $scope.eventsPending[eID] = e;
    $scope.eventCount++;
    return e;
  }

  $scope.messagesDisplayed = function() {
    return $('.messages .msglist-message').length
  }

  $scope.refresh = function() {
    var e = $scope.startEvent("Loading messages", null, "glyphicon-download");
    var url = $scope.host + 'api/messages'
    if($scope.startIndex > 0) {
      url += "?start=" + $scope.startIndex + "&limit=" + $scope.itemsPerPage;
    } else {
      url += "?limit=" + $scope.itemsPerPage;
    }
    $http.get(url).success(function(data) {
      $scope.messages = data.Messages;
      $scope.totalMessages = data.TotalMessageCount;
      $scope.countMessages = data.Messages.length;
      $scope.startMessages = $scope.startIndex;
      e.done();
    });
  }
  $scope.refresh();
  $interval(function () {
      if ($scope.startIndex == 0) {
          $scope.refresh();
      }
  }, 8000);

  $scope.showNewer = function() {
    $scope.startIndex -= $scope.itemsPerPage;
    if($scope.startIndex < 0) {
      $scope.startIndex = 0
    }
    $scope.refresh();
  }

  $scope.showUpdated = function(i) {
    $scope.itemsPerPage = parseInt(i, 10);
    if(typeof(Storage) !== "undefined") {
        localStorage.setItem("itemsPerPage", $scope.itemsPerPage)
    }

    $scope.startIndex = 0;
    $scope.refresh();
  }

  $scope.showOlder = function() {
    $scope.startIndex += $scope.itemsPerPage;
    $scope.refresh();
  }

  $scope.selectMessage = function (message) {
      $timeout(function () {
          $scope.resizePreview();
      }, 0);

      if ($scope.cache[message.Id]) {
          $scope.preview = $scope.cache[message.Id];
      } else {
          $scope.preview = message;
          var e = $scope.startEvent("Loading message", message.Id, "glyphicon-download-alt");
          $http.get($scope.host + 'api/messages/' + message.Id).success(function (data) {
              $scope.cache[message.Id] = data;

              data.previewHTML = $sce.trustAsHtml(data.HtmlBody);
              $scope.preview = data;
              e.done();
          });
      }
  }

  $scope.tryDecodeContent = function(message) {
    var charset = "UTF-8"
    if(message.Content.Headers["Content-Type"][0]) {
      // TODO
    }

    var content = message.Content.Body;
    var contentTransferEncoding = message.Content.Headers["Content-Transfer-Encoding"][0];

    if(contentTransferEncoding) {
      switch (contentTransferEncoding.toLowerCase()) {
        case 'quoted-printable':
          content = content.replace(/=[\r\n]+/gm,"");
          content = unescapeFromQuotedPrintableWithoutRFC2047(content, charset);
          break;
        case 'base64':
          // remove line endings to give original base64-encoded string
          content = content.replace(/\r?\n|\r/gm,"");
          content = unescapeFromBase64(content, charset);
          break;
      }
    }

    return content;
  }

  $scope.formatMessagePlain = function(message) {
    var body = message.TextBody || '';
    var escaped = $scope.escapeHtml(body);
    var formatted = escaped.replace(/(https?:\/\/)([-[\]A-Za-z0-9._~:/?#@!$()*+,;=%]|&amp;|&#39;)+/g, '<a href="$&" target="_blank">$&</a>');
    return $sce.trustAsHtml(formatted);
  }

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

  $scope.findMatchingMIME = function(part, mime) {
    // TODO cache results
    if(part.MIME) {
      for(var p in part.MIME.Parts) {
        if("Content-Type" in part.MIME.Parts[p].Headers) {
          if(part.MIME.Parts[p].Headers["Content-Type"].length > 0) {
            if(part.MIME.Parts[p].Headers["Content-Type"][0].match(mime + ";?.*")) {
              return part.MIME.Parts[p];
            } else if (part.MIME.Parts[p].Headers["Content-Type"][0].match(/multipart\/.*/)) {
              var f = $scope.findMatchingMIME(part.MIME.Parts[p], mime);
              if(f != null) {
                return f;
              }
            }
          }
        }
      }
    }
    return null;
  }
  $scope.hasHTML = function(message) {
      return !!message.HtmlBody;
  }

  $scope.hasText = function (message) {
      return !!message.TextBody;
  }

  $scope.getMessageHTML = function(message) {
    console.log(message);
    for(var header in message.Content.Headers) {
      if(header.toLowerCase() == 'content-type') {
        if(message.Content.Headers[header][0].match("text/html")) {
          return $scope.tryDecode(message.Content);
        }
      }
    }

    var l = $scope.findMatchingMIME(message, "text/html");
    if(l != null && l !== "undefined") {
      return $scope.tryDecode(l);
    }
  	return "<HTML not found>";
	}

  $scope.tryDecode = function(l){
    if(l.Headers && l.Headers["Content-Type"] && l.Headers["Content-Transfer-Encoding"]){
      return $scope.tryDecodeContent({Content:l});
    }else{
      return l.Body;
    }
  };
  $scope.date = function(timestamp) {
  	return (new Date(timestamp)).toString();
  };

  $scope.getSource = function(message) {
  	var source = "";
  	$.each(message.Content.Headers, function(k, v) {
  		source += k + ": " + v + "\n";
  	});
	source += "\n";
	source += message.Content.Body;
	return source;
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

function guid() {
    function s4() {
        return Math.floor((1 + Math.random()) * 0x10000)
                   .toString(16)
                   .substring(1);
    }
    return s4() + s4() + '-' + s4() + '-' + s4() + '-' +
           s4() + '-' + s4() + s4() + s4();
}

papercutApp.directive('ngKeyEnter', function () {
    return function (scope, element, attrs) {
        element.bind("keydown keypress", function (event) {
            if (event.which === 13) {
                scope.$apply(function () {
                    scope.$eval(attrs.ngKeyEnter);
                });
                event.preventDefault();
            }
        });
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
                return html.replace(/cid:([^"^'^\s^;^,^//^/<^/>]+)/gi, '/api/messages/' + messageId + '/contents/$1');
            }
        }
    };
}]);
