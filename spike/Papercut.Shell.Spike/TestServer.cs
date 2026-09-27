using System.Net;
using System.Net.Sockets;

namespace Papercut.Shell.Spike;

/// <summary>
/// Stands in for Papercut.Service: a local origin serving a page that reproduces the web UI's
/// email iframe (same sandbox attributes and injected base target as content-formatting.service.ts).
/// </summary>
internal sealed class TestServer : IDisposable
{
    private readonly HttpListener _listener;

    private TestServer(int port)
    {
        Origin = new Uri($"http://127.0.0.1:{port}/");
        _listener = new HttpListener();
        _listener.Prefixes.Add(Origin.ToString());
        _listener.Start();
        _ = Task.Run(Serve);
    }

    public Uri Origin { get; }

    public static TestServer Start()
    {
        var probe = new TcpListener(IPAddress.Loopback, 0);
        probe.Start();
        var port = ((IPEndPoint)probe.LocalEndpoint).Port;
        probe.Stop();

        return new TestServer(port);
    }

    public void Dispose() => _listener.Close();

    private async Task Serve()
    {
        while (_listener.IsListening)
        {
            HttpListenerContext context;

            try
            {
                context = await _listener.GetContextAsync();
            }
            catch (Exception) when (!_listener.IsListening)
            {
                return;
            }

            var path = context.Request.Url?.AbsolutePath ?? "/";
            var html = path == "/same-origin" ? SameOriginPage : MainPage.Replace("{ORIGIN}", Origin.ToString());
            var body = System.Text.Encoding.UTF8.GetBytes(html);

            context.Response.ContentType = "text/html; charset=utf-8";
            context.Response.ContentLength64 = body.Length;
            await context.Response.OutputStream.WriteAsync(body);
            context.Response.Close();
        }
    }

    private const string SameOriginPage =
        """
        <!doctype html><meta charset="utf-8"><title>same origin</title>
        <body style="font-family:system-ui;margin:16px">
        <h3>T1 passed: same-origin navigation stayed in the window.</h3>
        <a href="/">Back to the test page</a>
        </body>
        """;

    private const string MainPage =
        """
        <!doctype html>
        <html><head><meta charset="utf-8"><title>Papercut shell spike</title>
        <style>
          body { font-family: system-ui; margin: 16px; }
          iframe { width: 100%; height: 190px; border: 1px solid #888; }
          li { margin: 4px 0; }
          code { background: #eee; padding: 0 3px; }
        </style></head>
        <body>
        <h3>Top-level page (the Angular app's own frame)</h3>
        <ul>
          <li><a href="/same-origin">T1 same-origin link</a> &mdash; expect: allowed</li>
          <li><a href="https://example.com/?case=T2">T2 external link, same window</a> &mdash; expect: cancelled</li>
          <li><a href="https://example.com/?case=T3" target="_blank">T3 external link, target=_blank</a> &mdash; expect: new window handled</li>
          <li><button onclick="window.open('https://example.com/?case=T4')">T4 window.open</button> &mdash; expect: new window handled</li>
          <li><a href="mailto:someone@example.com?subject=T5">T5 mailto</a> &mdash; expect: cancelled</li>
          <li><a href="file:///etc/hosts">T6 file: link</a> &mdash; expect: cancelled</li>
          <li><button onclick="navigator.geolocation.getCurrentPosition(()=>{},()=>{})">T7 geolocation</button> &mdash; expect: denied</li>
        </ul>

        <h3>Email iframe &mdash; <code>sandbox="allow-same-origin allow-popups allow-popups-to-escape-sandbox"</code></h3>
        <iframe id="mail" sandbox="allow-same-origin allow-popups allow-popups-to-escape-sandbox"></iframe>
        <p>
          <button onclick="load(email)">Reload email</button>
          <button onclick="load(refresh)">I4 load an email with a 2s meta refresh</button> &mdash; expect: frame navigation cancelled
        </p>

        <script>
          const base = '<meta name="referrer" content="no-referrer"><base href="{ORIGIN}" target="_blank">';
          const email = base + `
            <body style="font-family:system-ui">
            <ul>
              <li><a href="https://example.com/?case=I1">I1 link, inherits base target=_blank</a> &mdash; expect: new window handled</li>
              <li><a href="https://example.com/?case=I2" target="_self">I2 link, target=_self</a> &mdash; expect: frame navigation cancelled</li>
              <li><a href="https://example.com/?case=I3" target="_top">I3 link, target=_top</a> &mdash; expect: blocked by sandbox</li>
              <li><form action="https://example.com/?case=I5" method="post" target="_self" style="display:inline"><button>I5 form post</button></form> &mdash; expect: blocked by sandbox</li>
            </ul></body>`;
          const refresh = base + '<meta http-equiv="refresh" content="2;url=https://example.com/?case=I4"><body style="font-family:system-ui">I4: refreshing to example.com in 2s&hellip;</body>';
          function load(html) { document.getElementById('mail').srcdoc = html; }
          load(email);
        </script>
        </body></html>
        """;
}
