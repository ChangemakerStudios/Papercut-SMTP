
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

                var head = $(element).contents().find('head');
                var hrefBase = document.createElement('base');
                hrefBase.target = '_blank';
                head.append(hrefBase);

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