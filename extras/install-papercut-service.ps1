# Define the service details
$serviceName = "Papercut.Smtp.Service"
$displayName = "Papercut SMTP Service"
$exeName = "Papercut.Service.exe"

# Get the current directory
$currentDirectory = (Get-Location).Path
$exePath = "$currentDirectory\$exeName"

# Run the script as administrator
If (-Not ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    $arguments = "& '" + $myInvocation.MyCommand.Definition + "'"
    Start-Process powershell -ArgumentList $arguments -Verb RunAs
    Exit
}

# Install the service using sc.exe
sc.exe create $serviceName binPath= $exePath DisplayName= "$displayName" start= auto

# Check if the service was created successfully
$service = Get-Service -Name $serviceName -ErrorAction SilentlyContinue

if ($service) {
    try {
        # Verify if the service is running
        $service = Get-Service -Name $serviceName
        if ($service.Status -eq 'Running') {
            Write-Host "Service $displayName ($serviceName) has been installed and started successfully."
        }
        else {
            Write-Host "Service $displayName ($serviceName) was installed but failed to start."
        }
    }
    catch {
        Write-Host "Service $displayName ($serviceName) was installed but failed to start. Error: $_"
    }
}
else {
    Write-Host "Failed to install the service $displayName ($serviceName)."
}

# Prompt to press any key to continue
Write-Host "Press any key to continue..."
$x = $host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")

Write-Host "Done."