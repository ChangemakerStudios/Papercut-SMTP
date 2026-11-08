# Troubleshooting

## I don't see any emails

1. **Is Papercut running?** The desktop app must be running, with its icon in the tray (or the [service](service.md) installed), to receive email.
2. **Check the logs** (**View Logs** in the tray menu) — you should see the SMTP server start (e.g. `Starting Smtp Server on 0.0.0.0:25`). If another program grabbed the port, Papercut will say so; pick a different port in Options.
3. **Confirm your app's SMTP settings** — host `localhost`, and the port must match Papercut's (default `25`; `2525` on macOS, in Docker and for the Windows Service).
4. **Send a test email directly** to isolate whether the problem is your app or Papercut:
   ```powershell
   Send-MailMessage -SmtpServer localhost -Port 25 -From "a@b" -To "c@d" -Subject "test" -Body "test"
   ```
5. **Sending from another machine, VM, or container?** Papercut accepts mail from other machines by default, so check your firewall first. If you set the IP to a specific address in Options, set it back to `Any`.

## "The IP and Port combination is in use"

Another program (often IIS SMTP, another mail catcher, or an antivirus proxy) is already listening on port 25. Change Papercut's port in **Options**, or stop the other program.

The service keeps retrying while its SMTP port is in use and logs `SMTP port ... is in use -- retrying until it is free`, so it starts receiving as soon as the other program stops.

## The API or MCP takes about 2 seconds to respond on `localhost`

On Windows, `localhost` tries IPv6 first. A service bound to `0.0.0.0` only listens on IPv4, so each new connection waits about 2 seconds for the IPv6 attempt to fail. Use `127.0.0.1` instead of `localhost`.

## The web UI is blank, or my own page gets 403 errors

Web pages on another origin are blocked. If the web UI is a blank page behind a reverse proxy, or your own page's calls fail, look in the logs for a line like:

```text
Blocked GET /main-RVU45O6T.js from https://mail.example.com
```

To allow that origin, or if a reverse proxy passes neither `Host` nor `X-Forwarded-Host`, see [Calling the API from a web page](service.md#calling-the-api-from-a-web-page).

A `403` saying Papercut doesn't answer to this host name, with a log line like `Blocked GET / for host mail.example.com`, means that name has to be listed. See [Reaching Papercut by host name](service.md#reaching-papercut-by-host-name).

## Do I need the Papercut Service?

**No.** The desktop app alone receives and displays email. The service is only for receiving email when the desktop app isn't running — see [Service & Web UI](service.md).

## HTML emails don't render / blank message view

On Windows, the Papercut window requires the **WebView2 Runtime**, preinstalled on current Windows. If it's missing, install it from [Microsoft's WebView2 page](https://developer.microsoft.com/en-us/microsoft-edge/webview2) and restart Papercut. Until then, **Open in Browser** in the tray menu shows the same UI.

## macOS won't open Papercut

The macOS build isn't signed yet, so macOS blocks the first launch. Go to **System Settings → Privacy & Security** and choose **Open Anyway**.

## Emails send fine but my app errors on authentication or TLS

Papercut accepts any credentials, but TLS is **off by default**. If your app *requires* an encrypted connection, either disable TLS in the app's dev configuration or [enable TLS in Papercut](smtp-tls-auth.md).

## Where are my emails stored?

As `.eml` files on disk. In the desktop app, choose **File → Open Data Folder**. To change the location, see [Desktop App](desktop.md#options).

## Docker: web UI loads but no emails arrive

Your app must send to the **container's SMTP port** (default `2525`, or whatever you mapped) — not port 25 on localhost, unless you mapped `-p 25:2525`. From other containers in the same compose network, use the service name as host. See [Docker](docker.md).

## Still stuck?

- Check the logs (**View Logs** in the desktop tray menu) or container logs (`docker logs papercut`) for errors
- Search [existing issues](https://github.com/ChangemakerStudios/Papercut-SMTP/issues)
- [Open a new issue](https://github.com/ChangemakerStudios/Papercut-SMTP/issues/new) with your setup details and log output
