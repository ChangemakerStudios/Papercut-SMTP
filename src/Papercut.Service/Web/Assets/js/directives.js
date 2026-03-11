
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

                var fontStyle = document.createElement('style');
                fontStyle.textContent = "@import url('https://fonts.googleapis.com/css2?family=Plus+Jakarta+Sans:wght@400;500;600;700&display=swap'); body { font-family: 'Plus Jakarta Sans', -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif; }";
                head.append(fontStyle);

                var body = $(element).contents().find('body');
                body.empty().append( htmlContent );

                var iframeDoc = body[0].ownerDocument;
                var resizeObserver = null;
                var measureRetries = 0;

                function measureHeight() {
                    var contentHeight = Math.max(
                        iframeDoc.documentElement.scrollHeight || 0,
                        iframeDoc.body.scrollHeight || 0,
                        $(iframeDoc.documentElement).height() || 0
                    );
                    if (contentHeight > 0) {
                        element.css('height', contentHeight + 100 + 'px');
                    }
                }

                // Use ResizeObserver for dynamic content changes (e.g. images loading, fonts rendering)
                if (typeof ResizeObserver !== 'undefined') {
                    resizeObserver = new ResizeObserver(function () {
                        measureHeight();
                    });
                    resizeObserver.observe(iframeDoc.documentElement);
                }

                // Measure after fonts load, with a timeout fallback in case fonts.ready stalls
                var fontTimeout = $timeout(measureHeight, 500);
                if (iframeDoc.fonts && iframeDoc.fonts.ready) {
                    iframeDoc.fonts.ready.then(function () {
                        $timeout.cancel(fontTimeout);
                        $timeout(measureHeight);
                    });
                }

                // Retry measurements to catch late-rendering content
                function retryMeasure() {
                    measureRetries++;
                    measureHeight();
                    if (measureRetries < 5) {
                        $timeout(retryMeasure, measureRetries * 200);
                    }
                }
                $timeout(retryMeasure, 100);

                // Clean up ResizeObserver when scope is destroyed
                scope.$on('$destroy', function () {
                    if (resizeObserver) {
                        resizeObserver.disconnect();
                    }
                });
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
                return html.replace(/cid:([^"^'^\s^;^,^//^/<^/>]+)/gi, function(match, cid) {
                    return '/api/messages/' + encodeURIComponent(messageId) + '/contents/' + encodeURIComponent(cid);
                });
            }
        }
    };
}]);