![Papercut Logo](https://raw.githubusercontent.com/ChangemakerStudios/Papercut/develop/graphics/PapercutLogo.png)<br>
*The Simple Desktop Email Helper*

[![Build status](https://ci.appveyor.com/api/projects/status/bs2asxoafdwbkcxa?svg=true)](https://ci.appveyor.com/project/Jaben/papercut-smtp)

## The problem
If you ever send emails from an application or website during development, you're familiar with the fear of an email being released into the wild. Are you positive none of the 'test' emails are addressed to colleagues or worse, customers? Of course, you can set up and maintain a test email server for development -- but that's a chore. Plus, the delay when waiting to view new test emails can radically slow your development cycle.

## Papercut SMTP to the rescue!
Papercut SMTP is a 2-in-1 quick email viewer AND built-in SMTP server (designed to receive messages only). Papercut SMTP doesn't enforce any restrictions on how you prepare your email, but it allows you to view the whole email-chilada: body, HTML, headers, and attachment right down to the naughty raw encoded bits. Papercut can be configured to run on startup and sit quietly (minimized in the tray) only providing a notification when a new message has arrived.

## Download Now
### [Download the Papercut.Setup.exe installer](https://github.com/ChangemakerStudios/Papercut-SMTP/releases)

## Requirements
Papercut SMTP UI Requires the "WebView2" Microsoft shared system component be installed on your system. If you have any problems getting it running go to this site: 
[WebView2 Download](https://developer.microsoft.com/en-us/microsoft-edge/webview2) and install it.

## Features
#### Instant Feedback When New Email Arrives
![Instant Feedback When New Email Arrives](https://changemakerstudios.us/content/images/2020/07/Papercut-2013.3-SS2.png)
#### Rich and Detailed View of Received Email
![Rich and Detailed View of Received Email](https://changemakerstudios.us/content/images/2020/07/Papercut-Main.png)
#### View and Download the Mime Sections of your Email
![View and Download the Mime Sections of your Email](https://changemakerstudios.us/content/images/2020/07/Papercut-Mime.png)
#### Raw View
![Raw View](https://changemakerstudios.us/content/images/2020/07/Papercut-Raw.png)
#### Logging View
![Logging View](https://changemakerstudios.us/content/images/2020/07/Papercut-Log.png)

## Papercut SMTP Service
Papercut SMTP has an optional HTTP server to receive emails even when the client is not running. It's installed by default with [Papercut.Setup.exe](https://github.com/ChangemakerStudios/Papercut/releases).
Alternatively, it can be run in an almost portable way by downloading [Papercut.Service.zip](https://github.com/ChangemakerStudios/Papercut/releases), unzipping and [following the service installation instructions](https://github.com/ChangemakerStudios/Papercut/tree/develop/src/Papercut.Service).

### Host in Docker

Optionally you can run Papercut SMTP Service in docker: [Papercut SMTP Service in Docker](https://hub.docker.com/r/changemakerstudiosus/papercut-smtp)

#### Pull Image:

```powershell
> docker pull changemakerstudiosus/papercut-smtp:latest
```

#### Run Papercut STMP Server Locally in Docker (HTTP Port :8080 and STMP port 25)
```powershell
docker run --name papercutsmtp -p 8080:80 -p 25:25 changemakerstudiosus/papercut-smtp:latest
```

The Papercut-SMTP Server Site will be accessible at http://localhost:8080.

## License
Papercut SMTP is Licensed under the [Apache License, Version 2.0](http://www.apache.org/licenses/LICENSE-2.0).
