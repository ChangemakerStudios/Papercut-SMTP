![Papercut Logo](https://raw.githubusercontent.com/ChangemakerStudios/Papercut/develop/graphics/PapercutLogo.png)<br>
*The Simple Desktop Email Helper*

[![Build and Release](https://github.com/ChangemakerStudios/Papercut-SMTP/actions/workflows/build.yml/badge.svg)](https://github.com/ChangemakerStudios/Papercut-SMTP/actions/workflows/build.yml)

## The problem
If you ever send emails from an application or website during development, you're familiar with the fear of an email being released into the wild. Are you positive none of the 'test' emails are addressed to colleagues or worse, customers? Of course, you can set up and maintain a test email server for development -- but that's a chore. Plus, the delay when waiting to view new test emails can radically slow your development cycle.

<img src="https://raw.githubusercontent.com/ChangemakerStudios/Papercut-SMTP/refs/heads/develop/graphics/papercut-choice.png" width="400" />

## Papercut SMTP to the rescue!
Papercut SMTP is a 2-in-1 quick email viewer AND built-in SMTP server (designed to receive messages only). Papercut SMTP doesn't enforce any restrictions on how you prepare your email, but it allows you to view the whole email-chilada: body, HTML, headers, and attachment right down to the naughty raw encoded bits. Papercut can be configured to run on startup and sit quietly (minimized in the tray) only providing a notification when a new message has arrived.

## Download Desktop App Now
#### Download the 64-bit [PapercutSMTP-win-X64-stable-Setup.exe](https://github.com/ChangemakerStudios/Papercut-SMTP/releases) desktop installer in releases.
#### Download the 32-bit [PapercutSMTP-win-x86-stable-Setup.exe](https://github.com/ChangemakerStudios/Papercut-SMTP/releases) desktop installer in releases.

**For installation options, command-line parameters, and silent/unattended installation instructions, see the [Installation Guide](installation/README.md).**

## Requirements
Papercut SMTP UI Requires the "WebView2" Microsoft shared system component to be installed on your system. If you have any problems getting it running go to this site:
[WebView2 Download](https://developer.microsoft.com/en-us/microsoft-edge/webview2) and install it.

## Features
#### Instant Feedback When New Email Arrives
![Instant Feedback When New Email Arrives](https://github.com/ChangemakerStudios/Papercut-SMTP/blob/develop/graphics/PapercutV7-Notification-1.png?raw=true)
#### Rich and Detailed View of Received Email
![Rich and Detailed View of Received Email](https://raw.githubusercontent.com/ChangemakerStudios/Papercut-SMTP/refs/heads/develop/graphics/PapercutV7-Main-1.png)
#### View and Download the Mime Sections of your Email
![View and Download the Mime Sections of your Email](https://changemakerstudios.us/content/images/2020/07/Papercut-Mime.png)
#### Raw View
![Raw View](https://changemakerstudios.us/content/images/2020/07/Papercut-Raw.png)
#### Logging View
![Logging View](https://changemakerstudios.us/content/images/2020/07/Papercut-Log.png)

## (Optional) Download Papercut SMTP Service
Papercut SMTP has an optional HTTP server to receive emails even when the client is not running.
It can be run in an almost portable way by downloading [Papercut.Smtp.Service.*.zip](https://github.com/ChangemakerStudios/Papercut-SMTP/releases), unzipping, and installing as a Windows Service.

### Installing Papercut SMTP Service

1. **Download** the appropriate [Papercut.Smtp.Service.*.zip](https://github.com/ChangemakerStudios/Papercut-SMTP/releases) for your system (win-x64 or win-x86)
2. **Extract** the zip file to your desired location
3. **Run the installation script** (requires administrator privileges):
   - **Option A:** Double-click `install-papercut-service.bat`
   - **Option B:** Run `install-papercut-service.ps1` in PowerShell
4. The service will be installed and configured to **start automatically** on system boot

**To uninstall:** Run `uninstall-papercut-service.bat` or `uninstall-papercut-service.ps1`

**For complete Service configuration and Docker deployment instructions, see the [Service README](src/Papercut.Service/Readme.md).**

### Host in Docker

Optionally run Papercut SMTP Service in Docker: [Papercut SMTP on Docker Hub](https://hub.docker.com/r/changemakerstudiosus/papercut-smtp)

**Quick start:**
```powershell
docker pull changemakerstudiosus/papercut-smtp:latest
docker run -d -p 37408:8080 -p 2525:2525 changemakerstudiosus/papercut-smtp:latest
```

Access at: **http://localhost:37408** | Send emails to: **localhost:2525**

> **Note:** Docker uses non-privileged ports by default (SMTP: 2525, HTTP: 8080). See the [Service README](src/Papercut.Service/Readme.md#option-3-run-in-docker) for configuration options, Docker Compose examples, and troubleshooting.

## License
Papercut SMTP is Licensed under the [Apache License, Version 2.0](http://www.apache.org/licenses/LICENSE-2.0).
