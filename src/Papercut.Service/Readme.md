## How to Run Papercut.Service

### Option 1: Run as Console Application
Run from the command line by entering:
```
Papercut.Service.exe
```

### Option 2: Install as Windows Service (Recommended)

The service can be installed to run automatically on Windows startup.

#### Using the Installation Scripts:

**For PowerShell users:**
```powershell
.\install-papercut-service.ps1
```

**For Command Prompt users:**
```cmd
install-papercut-service.bat
```

Both scripts will:
- Require administrator privileges (will prompt if needed)
- Validate that Papercut.Service.exe exists
- Install the service with automatic startup configured
- Start the service immediately

#### Uninstalling the Service:

**PowerShell:**
```powershell
.\uninstall-papercut-service.ps1
```

**Command Prompt:**
```cmd
uninstall-papercut-service.bat
```

#### Manual Installation (Advanced):
You can also install the service manually using `sc.exe`:
```cmd
sc.exe create Papercut.Smtp.Service binPath= "C:\path\to\Papercut.Service.exe" DisplayName= "Papercut SMTP Service" start= auto
sc.exe start Papercut.Smtp.Service
```

## How to Configure Papercut.Service

_Papercut.Service_ does not need manual configuration. When the service and the client (_Papercut.exe_) processes are run at the same time, they will automatically synchronize their configurations. For example, when the SMTP settings is modified in the Papercut UI options, the service will automatically update itself and save these changes. Rule changes work the same way.

If manual confiugration is needed, the configuration file for _Papercut.Service_ can be found in the same directory as the _Papercut.Service.exe_. `Papercut.Service.json` contains the configuration with comments outlining options. Note that any changes will require the service to be restarted to take effect.
