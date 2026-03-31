
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
            var resizeObserver = null;

            // Watch for content changes instead of relying on the load event,
            // which may not fire reliably when src is already about:blank.
            scope.$watch(attrs.bodyHtml, function (newVal) {
                if (!newVal) return;
                var messageId = scope.$eval(attrs.contentLinkMessageId);
                var htmlContent = $sce.getTrustedHtml(newVal);
                htmlContent = stripDangerousTags(htmlContent);
                htmlContent = replaceContentLinks(htmlContent, messageId);

                // Write directly to the iframe document to avoid load-event race conditions
                var iframeEl = element[0];
                var iframeDoc = iframeEl.contentDocument || (iframeEl.contentWindow && iframeEl.contentWindow.document);
                if (!iframeDoc) return;

                var fullHtml = '<!DOCTYPE html><html><head>'
                    + '<base target="_blank">'
                    + '<style>@import url(\'https://fonts.googleapis.com/css2?family=Plus+Jakarta+Sans:wght@400;500;600;700&display=swap\'); body { font-family: \'Plus Jakarta Sans\', -apple-system, BlinkMacSystemFont, \'Segoe UI\', sans-serif; margin: 0; }</style>'
                    + '</head><body>' + htmlContent + '</body></html>';

                iframeDoc.open();
                iframeDoc.write(fullHtml);
                iframeDoc.close();

                var measureRetries = 0;

                function measureHeight() {
                    if (!iframeDoc.documentElement || !iframeDoc.body) return;
                    var contentHeight = Math.max(
                        iframeDoc.documentElement.scrollHeight || 0,
                        iframeDoc.body.scrollHeight || 0
                    );
                    if (contentHeight > 0) {
                        element.css('height', contentHeight + 50 + 'px');
                    }
                }

                // Clean up previous observer if re-rendering
                if (resizeObserver) {
                    resizeObserver.disconnect();
                    resizeObserver = null;
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
            });

            // Clean up ResizeObserver when scope is destroyed
            scope.$on('$destroy', function () {
                if (resizeObserver) {
                    resizeObserver.disconnect();
                }
            });


            function stripDangerousTags(html) {
                // Strip truly dangerous tags (script, iframe, etc.) but preserve <style> tags
                // which are needed for email layout (e.g. MJML responsive styles).
                // Style tags are safe inside a same-origin iframe since they only affect iframe content.
                var tagStarts = /\<\s*(script|iframe|frameset|applet|object)(?=\s|\>)/gi;
                var tagEnds = /\<\s*\/\s*(script|iframe|frameset|applet|object)\s*\>/gi;
                // Strip <link> tags that could load external resources (except stylesheets are OK)
                var linkTags = /\<\s*link(?=\s)[^>]*>/gi;
                var links = /((src|href)\s*=["']?\s*)javascript:/gi;

                return html.replace(tagStarts, '<div style="display:none" ')
                           .replace(tagEnds, '</div>')
                           .replace(linkTags, '')
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