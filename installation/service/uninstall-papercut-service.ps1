# Define the service details
$serviceName = "Papercut.Smtp.Service"
$displayName = "Papercut SMTP Service"

# Run the script as administrator if not already
If (-Not ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    Write-Host "Requesting administrator privileges..."
    $arguments = "-NoProfile -ExecutionPolicy Bypass -File `"$($MyInvocation.MyCommand.Path)`""
    Start-Process powershell -ArgumentList $arguments -Verb RunAs
    Exit
}

Write-Host "Uninstalling Papercut SMTP Service..." -ForegroundColor Cyan
Write-Host ""

# Check if the service exists
$service = Get-Service -Name $serviceName -ErrorAction SilentlyContinue
if (-Not $service) {
    Write-Host "Service '$serviceName' is not installed." -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Press any key to exit..."
    $null = $host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
    Exit 0
}

Write-Host "Found service '$displayName' (Status: $($service.Status))" -ForegroundColor Gray

try {
    # Stop the service if it's running
    if ($service.Status -eq 'Running') {
        Write-Host "Stopping service..." -ForegroundColor Gray
        Stop-Service -Name $serviceName -Force -ErrorAction Stop

        # Wait for service to fully stop
        $timeout = 10
        $elapsed = 0
        while ((Get-Service -Name $serviceName).Status -ne 'Stopped' -and $elapsed -lt $timeout) {
            Start-Sleep -Seconds 1
            $elapsed++
        }

        if ((Get-Service -Name $serviceName).Status -ne 'Stopped') {
            Write-Host "WARNING: Service did not stop within $timeout seconds" -ForegroundColor Yellow
        }
        else {
            Write-Host "Service stopped successfully." -ForegroundColor Gray
        }
    }

    # Delete the service using sc.exe
    Write-Host "Removing service..." -ForegroundColor Gray
    $deleteResult = sc.exe delete $serviceName

    if ($LASTEXITCODE -ne 0) {
        Write-Host "ERROR: Failed to delete service. sc.exe returned error code: $LASTEXITCODE" -ForegroundColor Red
        Write-Host $deleteResult -ForegroundColor Red
        Write-Host ""
        Write-Host "Press any key to exit..."
        $null = $host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
        Exit $LASTEXITCODE
    }

    # Verify service was deleted
    Start-Sleep -Seconds 1
    $verifyService = Get-Service -Name $serviceName -ErrorAction SilentlyContinue

    if ($verifyService) {
        Write-Host ""
        Write-Host "WARNING: Service may still exist. Please check Services (services.msc)" -ForegroundColor Yellow
    }
    else {
        Write-Host ""
        Write-Host "SUCCESS: Service '$displayName' has been uninstalled successfully!" -ForegroundColor Green
    }
}
catch {
    Write-Host ""
    Write-Host "ERROR: Failed to uninstall service." -ForegroundColor Red
    Write-Host "Error: $_" -ForegroundColor Red
    Write-Host ""
    Write-Host "Press any key to exit..."
    $null = $host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
    Exit 1
}

Write-Host ""
Write-Host "Press any key to exit..."
$null = $host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")