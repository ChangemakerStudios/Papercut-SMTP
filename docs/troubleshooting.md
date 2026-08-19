# Troubleshooting

## I don't see any emails

1. **Is Papercut running?** The desktop app must be open (or the [service](service.md) installed) to receive email.
2. **Check the Logs view** — you should see the SMTP server listening (e.g. `127.0.0.1:25`). If another program grabbed the port, Papercut will say so; pick a different port in Options.
3. **Confirm your app's SMTP settings** — host `localhost`, and the port must match Papercut's (default `25`; Docker default `2525`).
4. **Send a test email directly** to isolate whether the problem is your app or Papercut:
   ```powershell
   Send-MailMessage -SmtpServer localhost -Port 25 -From "a@b" -To "c@d" -Subject "test" -Body "test"
   ```
5. **Sending from another machine, VM, or container?** Papercut defaults to `127.0.0.1` — localhost only. Set the IP to `Any` in Options, and check your firewall.

## "The IP and Port combination is in use"

Another program (often IIS SMTP, another mail catcher, or an antivirus proxy) is already listening on port 25. Change Papercut's port in **Options**, or stop the other program.

## Do I need the Papercut Service?

**No.** The desktop app alone receives and displays email. The service is only for receiving email when the desktop app isn't running — see [Service & Web UI](service.md).

## HTML emails don't render / blank message view

The HTML view requires the **WebView2 Runtime**, preinstalled on current Windows. If it's missing, install it from [Microsoft's WebView2 page](https://developer.microsoft.com/en-us/microsoft-edge/webview2) and restart Papercut.

## Emails send fine but my app errors on authentication or TLS

Papercut accepts any credentials, but TLS is **off by default**. If your app *requires* an encrypted connection, either disable TLS in the app's dev configuration or [enable TLS in Papercut](smtp-tls-auth.md).

## Where are my emails stored?

As `.eml` files on disk. In the desktop app, right-click a message → open its containing folder. The storage location is configurable in Options.

## Docker: web UI loads but no emails arrive

Your app must send to the **container's SMTP port** (default `2525`, or whatever you mapped) — not port 25 on localhost, unless you mapped `-p 25:2525`. From other containers in the same compose network, use the service name as host. See [Docker](docker.md).

## Still stuck?

- Check the **Logs** view (desktop) or container logs (`docker logs papercut`) for errors
- Search [existing issues](https://github.com/ChangemakerStudios/Papercut-SMTP/issues)
- [Open a new issue](https://github.com/ChangemakerStudios/Papercut-SMTP/issues/new) with your setup details and log output
