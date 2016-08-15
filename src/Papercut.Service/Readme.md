## How to Run Papercut.Service

- Option 1:
 Run from the command line by entering: `Papercut.Service.exe`.
- Option 2: Install as a service by entering `Install-Papercut-Service.bat` and the service will be installed. Enter `Papercut.Service.exe install --sudo` to immediately start the service.
- 
A listing of all command line options are avaliable by entering: `Papercut.Service.exe help`

## How to Configure Papercut.Service

_Papercut.Service_ should not need manual configuration. When the service and the client (_Papercut.exe_) processes, are run at the same time, they will sync their configurations automatically. For example, when the SMTP settings is modified in the UI options, the service will automatically update with these changes. Same thing happens with rule changes.

 But, the configuration file for Papercut.Service is located in the same directory as the _Papercut.Service.exe_: `Papercut.Service.json` -- but requires the service to be restarted if modified. 
