## How to Run Papercut.Service

- Option 1:
 Run from the command line: "Papercut.Service.exe" runs the process directly from the the command line.	Then you can configure the Service by running the Papercut.exe.
- Option 2: Install as a service: From command line run the "Install-Papercut-Service.bat" and it will install using one command: "Papercut.Service.exe install --sudo" and start the service.	Then you can configure the Service by running the Papercut.exe.

## How to Configure Papercut.Service

A listing of command line option is avaliable by entering: 

` Papercut.Service.exe help`

Papercut.Service does not require manual editing of files to run or configure. The Papercut.exe and Papercut.Service.exe processes, when run at the same time, will communicate and handle configuration automatically. e.g. When you change the SMTP settings in the UI options, it automatically pushes those changes (and saves them) in the Service. But, if you do need to manually configure, all the settings are in the default Papercut.Service.json file with comments.
