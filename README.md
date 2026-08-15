![Papercut Logo](https://raw.githubusercontent.com/ChangemakerStudios/Papercut/develop/graphics/PapercutLogo.png)<br>
*The Simple Desktop Email Receiver*

[![Build and Release](https://github.com/ChangemakerStudios/Papercut-SMTP/actions/workflows/build.yml/badge.svg)](https://github.com/ChangemakerStudios/Papercut-SMTP/actions/workflows/build.yml)
[![GitHub release](https://img.shields.io/github/v/release/ChangemakerStudios/Papercut-SMTP?label=release)](https://github.com/ChangemakerStudios/Papercut-SMTP/releases/latest)
[![Docker Pulls](https://img.shields.io/docker/pulls/changemakerstudiosus/papercut-smtp?logo=docker)](https://hub.docker.com/r/changemakerstudiosus/papercut-smtp)

Papercut SMTP is a 2-in-1 quick email viewer **and** built-in SMTP server for development. Point your application at `localhost:25` and every email it sends is caught, displayed instantly, and **never delivered anywhere** — body, HTML, headers, attachments, and raw bits included.

![Rich and Detailed View of Received Email](https://raw.githubusercontent.com/ChangemakerStudios/Papercut-SMTP/refs/heads/develop/graphics/PapercutV7-Main-1.png)

## Install

Download the desktop installer (always the latest version):

- **64-bit**: [PapercutSMTP-win-x64-stable-Setup.exe](https://github.com/ChangemakerStudios/Papercut-SMTP/releases/latest/download/PapercutSMTP-win-x64-stable-Setup.exe)
- **32-bit**: [PapercutSMTP-win-x86-stable-Setup.exe](https://github.com/ChangemakerStudios/Papercut-SMTP/releases/latest/download/PapercutSMTP-win-x86-stable-Setup.exe)
- **ARM64**: [PapercutSMTP-win-arm64-stable-Setup.exe](https://github.com/ChangemakerStudios/Papercut-SMTP/releases/latest/download/PapercutSMTP-win-arm64-stable-Setup.exe)

Or via WinGet (may lag behind the latest release):

```powershell
winget install ChangemakerStudios.Papercut-SMTP
```

Run it — Papercut is immediately listening on `127.0.0.1:25`. Configure your app to send there and you're done.

## Docker

```bash
docker run -d -p 8080:8080 -p 2525:2525 changemakerstudiosus/papercut-smtp:latest
```

Web UI at **http://localhost:8080**, SMTP on **localhost:2525**. Details on [Docker Hub](https://hub.docker.com/r/changemakerstudiosus/papercut-smtp).

## Documentation

**[www.papercut-smtp.com](https://www.papercut-smtp.com/)** — full documentation:

- [How It Works](https://www.papercut-smtp.com/how-it-works/) — what Papercut is (and isn't), in two minutes
- [Getting Started](https://www.papercut-smtp.com/getting-started/) — install, first run, first test email
- [Send Email from Your App](https://www.papercut-smtp.com/send-from-your-app/) — copy-paste config for .NET, Node, Python, PHP, Java, Ruby
- [Desktop App](https://www.papercut-smtp.com/desktop/) · [Service & Web UI](https://www.papercut-smtp.com/service/) · [Docker](https://www.papercut-smtp.com/docker/) · [TLS & Auth](https://www.papercut-smtp.com/smtp-tls-auth/)
- [Troubleshooting](https://www.papercut-smtp.com/troubleshooting/)

## Release History

See [ReleaseNotes.md](ReleaseNotes.md) for the full release history.

## Star History

<a href="https://star-history.dera.page/#ChangemakerStudios/Papercut-SMTP&type=date&legend=bottom-right">
  <picture>
    <source media="(prefers-color-scheme: dark)" srcset="https://star-history.dera.page/svg?repos=ChangemakerStudios/Papercut-SMTP&type=date&theme=dark&legend=top-left" />
    <source media="(prefers-color-scheme: light)" srcset="https://star-history.dera.page/svg?repos=ChangemakerStudios/Papercut-SMTP&type=date&legend=top-left" />
    <img alt="Star History Chart" src="https://star-history.dera.page/svg?repos=ChangemakerStudios/Papercut-SMTP&type=date&legend=top-left" />
  </picture>
</a>

## License

Papercut SMTP is licensed under the [Apache License, Version 2.0](http://www.apache.org/licenses/LICENSE-2.0).
