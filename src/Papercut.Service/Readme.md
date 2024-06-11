## How to Run Papercut.Service

- Option 1:
Run directly by running `Papercut.Service.exe`.
- Option 2: Install as a service by enering `Papercut.Service.exe install --sudo` to immediately start the service (you can easily create a shortcut for it and/or save it as `Install-Papercut-Service.bat`).

A listing of all command line options are avaliable by running `Papercut.Service.exe help`.

## How to Configure Papercut.Service

_Papercut.Service_ does not need manual configuration. When the service and the client (_Papercut.exe_) processes are run at the same time, they will automatically synchronize their configurations. For example, when the SMTP settings is modified in the Papercut UI options, the service will automatically update itself and save these changes. Rule changes work the same way.

If manual configuration is needed, the configuration file for _Papercut.Service_ can be found in the same directory as the _Papercut.Service.exe_. `Papercut.Service.Settings.json` contains the configuration with comments outlining options. Note that any changes will require the service to be restarted to take effect.

## How to Exit Papercut.Service
If run as a client, just close the console window.
