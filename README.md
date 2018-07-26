![Papercut Logo](https://raw.githubusercontent.com/ChangemakerStudios/Papercut/develop/graphics/PapercutLogo.png)
The Simple SMTP Desktop Email Receiver

[![Build status](https://ci.appveyor.com/api/projects/status/bs2asxoafdwbkcxa?svg=true)](https://ci.appveyor.com/project/Jaben/papercut)
[![Build Status](https://travis-ci.org/jijiechen/Papercut.svg?branch=feature%2Fnetcore)](https://travis-ci.org/jijiechen/Papercut)
[![Gitter](https://badges.gitter.im/Join%20Chat.svg)](https://gitter.im/Jaben/Papercut?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge)
[![Say Thanks!](https://img.shields.io/badge/Say%20Thanks-!-1EAEDB.svg)](https://saythanks.io/to/Jaben)


## What it does
Ever need to test emails from an application or web site but don't want them accidently being sent or having to deal with the hassle of setting up a test email server? Papercut is a quick email viewer with a built-in SMTP server designed to only receive messages. It doesn't enforce any restrictions how you send your email. It allows you to view the whole email-chilada: body, html, headers, attachment down to the naughty raw bits. It can be set to run on startup and sits quietly minimized in the tray giving you a balloon popup when a new message arrives.

The dotnet core based version of Papercut is a cross platform, Docker images are also available.


## Download Now
The .netcore based applications are not released yet. 
You can download the [released versions](https://github.com/ChangemakerStudios/Papercut/releases) or try the [nightly builds](https://jijiechen.github.io/Papercut/) at this time.

You can also use Papercut directly with Docker: 
```
    docker run --name=papercut -p 25:25 -p 37408:37408 jijiechen/papercut:latest
```

## Development

In development mode, the Papercut.Service will defaultly listen SMTP service on port `2525`. You can use the `send-test-mail.js` in the root directory to test Papercut.Service.
```
    npm install nodemailer
    node ./send-test-mail.js
```

For the desktop app, you run it by executing the `start-electron-app` script in the `src/Papercut.Desktop` directory:
```
    cd src/Papercut.Desktop
    ./start-electron.sh   # on Windows, use .\start-electron.bat 
```

To attach and debug the Electron app, just launch the development tool and debug the web page in electron.
If you'd like to debug the backend Papercut when running in electron desktop mode, add a `DEBUG_PAPAERCUT` environment variable and give it a value before launching the desktop app, the program will wait 30s for the debugger to attach. 


## License
Papercut is Licensed under the [Apache License, Version 2.0](http://www.apache.org/licenses/LICENSE-2.0).
