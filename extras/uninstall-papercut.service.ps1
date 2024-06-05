# Define the service details
$serviceName = "Papercut.Smtp.Service"

# Run the script as administrator
If (-Not ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    $arguments = "& '" + $myInvocation.MyCommand.Definition + "'"
    Start-Process powershell -ArgumentList $arguments -Verb RunAs
    Exit
}

# Check if the service exists
$service = Get-Service -Name $serviceName -ErrorAction SilentlyContinue
if ($service) {
    try {
        # Stop the service if it's running
        if ($service.Status -eq 'Running') {
            Stop-Service -Name $serviceName -Force
        }
        
        # Delete the service using sc.exe
        sc.exe delete $serviceName
        
        Write-Host "Service $serviceName has been deleted successfully."
    }
    catch {
        Write-Host "Failed to delete the service $serviceName. Error: $_"
    }
}
else {
    Write-Host "Service $serviceName does not exist."
}

# Prompt to press any key to continue
Write-Host "Press any key to continue..."
$x = $host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")

Write-Host "Done."