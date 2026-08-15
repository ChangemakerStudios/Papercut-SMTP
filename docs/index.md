# Papercut SMTP

**The simple email receiver and viewer for development.** Papercut SMTP is a 2-in-1 quick email viewer *and* built-in SMTP server. It catches every email your application sends — and delivers none of them.

[![Build and Release](https://github.com/ChangemakerStudios/Papercut-SMTP/actions/workflows/build.yml/badge.svg)](https://github.com/ChangemakerStudios/Papercut-SMTP/actions/workflows/build.yml)
[![GitHub release](https://img.shields.io/github/v/release/ChangemakerStudios/Papercut-SMTP?label=release)](https://github.com/ChangemakerStudios/Papercut-SMTP/releases/latest)
[![Docker Pulls](https://img.shields.io/docker/pulls/changemakerstudiosus/papercut-smtp?logo=docker)](https://hub.docker.com/r/changemakerstudiosus/papercut-smtp)

## The Problem

If you send emails from an application during development, you know the fear of a test email escaping into the wild. Are you *positive* none of those "test" emails are addressed to colleagues — or worse, customers? Setting up and maintaining a real test email server is a chore, and waiting for test emails to arrive slows your development cycle.

## The Solution

Point your application at Papercut SMTP instead of a real mail server. It accepts every message instantly, stores it locally, and shows it to you — body, HTML, headers, attachments, right down to the raw encoded bits. Nothing is ever delivered.

![Rich and Detailed View of Received Email](https://raw.githubusercontent.com/ChangemakerStudios/Papercut-SMTP/refs/heads/develop/graphics/PapercutV7-Main-1.png)

## Features

- **Instant notifications** when a new email arrives — Papercut sits quietly in the tray until it has something to show you
- **Full email inspection** — rendered HTML, plain text body, headers, MIME parts, attachments, and raw source
- **Zero configuration** — start the app and it's already listening on `127.0.0.1:25`
- **Optional background service** with a browser-based web UI, for receiving email even when the desktop app isn't running
- **Docker image** for containerized development environments and CI
- **Rules** — automatically forward or relay received messages when needed
- **TLS/STARTTLS and SMTP AUTH** support for testing secure email flows

## Quick Start

1. [Install Papercut SMTP](getting-started.md) and run it — it immediately listens on `127.0.0.1:25`.
2. [Point your application](send-from-your-app.md) at host `localhost`, port `25`, no credentials.
3. Send an email from your app — it pops up in Papercut. That's it.

!!! tip "New to Papercut?"
    Read [How It Works](how-it-works.md) — a two-minute explanation of what Papercut is (and isn't).

## License

Papercut SMTP is licensed under the [Apache License, Version 2.0](https://www.apache.org/licenses/LICENSE-2.0).
