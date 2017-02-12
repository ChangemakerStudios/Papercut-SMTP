## How to Run Papercut.Service

- Option 1:
Run from the command line by entering: `Papercut.Service.exe`
- Option 2: Install as a service with the command `Install-Papercut-Service.bat` and the service will be installed. Enter `Papercut.Service.exe install --sudo` to immediately start the service.

A listing of all command line options are avaliable by entering: `Papercut.Service.exe help`

## How to Configure Papercut.Service

_Papercut.Service_ does not need manual configuration. When the service and the client (_Papercut.exe_) processes are run at the same time, they will automatically synchronize their configurations. For example, when the SMTP settings is modified in the Papercut UI options, the service will automatically update itself and save these changes. Rule changes work the same way.

If manual confiugration is needed, the configuration file for _Papercut.Service_ can be found in the same directory as the _Papercut.Service.exe_. `Papercut.Service.json` contains the configuration with comments outlining options. Note that any changes will require the service to be restarted to take effect.
