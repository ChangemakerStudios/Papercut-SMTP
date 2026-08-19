# How It Works

The most common question about Papercut SMTP: *"How do I actually use this thing?"* Here's the two-minute version.

## Papercut is a mail server — a fake one

When your application sends an email, it doesn't magically arrive at the recipient. Your app connects to an **SMTP server** and hands the message over, and the server takes care of delivery.

Papercut SMTP **takes the place of that server**. It speaks the same SMTP protocol, so your application can't tell the difference — but instead of delivering messages, Papercut stores every one of them locally and shows them to you.

``` mermaid
graph LR
    A[Your Application] -- "SMTP<br/>localhost:25" --> B[Papercut SMTP]
    B --> C[Displayed in Papercut<br/>nothing is delivered]
```

This means:

- **Papercut is not an email client.** You don't send emails *to* Papercut's address, and you don't need an account.
- **The To address is irrelevant.** Send to `ceo@bigcorp.com`, `test@example.com`, anyone — Papercut accepts every recipient and delivers to none of them. It is *impossible* for a test email to escape.
- **No credentials needed.** Papercut accepts connections without authentication (and if your app insists on authenticating, Papercut accepts any username/password).

## What you configure

Exactly one thing: your application's outgoing mail settings. Wherever your app configures its SMTP server — `appsettings.json`, `.env`, `php.ini`, a config UI — set:

| Setting | Value |
|---------|-------|
| Host / Server | `localhost` (or `127.0.0.1`) |
| Port | `25` (Papercut's default) |
| Username / Password | *(leave empty — anything is accepted)* |
| Encryption | None (or STARTTLS if [configured](smtp-tls-auth.md)) |

See [Send Email from Your App](send-from-your-app.md) for copy-paste examples in C#, Node.js, Python, PHP, Java, and Ruby.

## Where emails go

Received messages are stored as standard `.eml` files on disk — no database. The desktop app lists them the moment they arrive, complete with a tray notification. Right-click a message and choose **Open Containing Folder** to get at the raw files.

## Desktop app vs. service

- **Papercut SMTP (desktop app)** — the normal way to use Papercut. It's both the SMTP server and the viewer. If the app is running, you're catching email. Nothing else is required.
- **Papercut SMTP Service** *(optional)* — a background Windows Service (or [Docker container](docker.md)) that receives email even when the desktop app is closed, with a [browser-based UI](service.md). Use it for shared dev servers, containers, or always-on setups.

!!! note "You do **not** need the service for normal desktop use."
    The desktop app alone is a complete SMTP server + viewer.
