# Define the service details
$serviceName = "Papercut.Smtp.Service"
$displayName = "Papercut SMTP Service"
$description = "Papercut SMTP - A development SMTP server for viewing and testing email functionality"
$exeName = "Papercut.Service.exe"

# Get the script directory (works even when run as admin)
$scriptPath = if ($PSScriptRoot) { $PSScriptRoot } else { Split-Path -Parent $MyInvocation.MyCommand.Path }
$exePath = Join-Path $scriptPath $exeName

# Run the script as administrator if not already
If (-Not ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    Write-Host "Requesting administrator privileges..."
    $arguments = "-NoProfile -ExecutionPolicy Bypass -File `"$($MyInvocation.MyCommand.Path)`""
    Start-Process powershell -ArgumentList $arguments -Verb RunAs
    Exit
}

Write-Host "Installing Papercut SMTP Service..." -ForegroundColor Cyan
Write-Host ""

# Validate that the executable exists
if (-Not (Test-Path $exePath)) {
    Write-Host "ERROR: Could not find $exeName at path: $exePath" -ForegroundColor Red
    Write-Host "Please ensure the script is run from the directory containing $exeName" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Press any key to exit..."
    $null = $host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
    Exit 1
}

# Check if service already exists
$existingService = Get-Service -Name $serviceName -ErrorAction SilentlyContinue
if ($existingService) {
    Write-Host "Service '$serviceName' already exists." -ForegroundColor Yellow
    Write-Host "Please uninstall the existing service first using uninstall-papercut-service.ps1" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Press any key to exit..."
    $null = $host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
    Exit 1
}

# Install the service using sc.exe (quotes handle paths with spaces)
Write-Host "Creating service '$serviceName'..." -ForegroundColor Gray
$createResult = sc.exe create $serviceName binPath= "`"$exePath`"" DisplayName= "$displayName" start= auto

if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Failed to create service. sc.exe returned error code: $LASTEXITCODE" -ForegroundColor Red
    Write-Host $createResult -ForegroundColor Red
    Write-Host ""
    Write-Host "Press any key to exit..."
    $null = $host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
    Exit $LASTEXITCODE
}

# Set the service description
Write-Host "Setting service description..." -ForegroundColor Gray
sc.exe description $serviceName $description | Out-Null

# Start the service
Write-Host "Starting service..." -ForegroundColor Gray
try {
    Start-Service -Name $serviceName -ErrorAction Stop

    # Wait a moment and verify service is running
    Start-Sleep -Seconds 2
    $service = Get-Service -Name $serviceName

    if ($service.Status -eq 'Running') {
        Write-Host ""
        Write-Host "SUCCESS: Service '$displayName' has been installed and started successfully!" -ForegroundColor Green
        Write-Host "Service Name: $serviceName" -ForegroundColor Gray
        Write-Host "Executable: $exePath" -ForegroundColor Gray
        Write-Host "Status: Running" -ForegroundColor Gray
    }
    else {
        Write-Host ""
        Write-Host "WARNING: Service was installed but is not running (Status: $($service.Status))" -ForegroundColor Yellow
        Write-Host "You may need to start it manually via Services (services.msc)" -ForegroundColor Yellow
    }
}
catch {
    Write-Host ""
    Write-Host "WARNING: Service was installed but failed to start." -ForegroundColor Yellow
    Write-Host "Error: $_" -ForegroundColor Red
    Write-Host "You may need to start it manually via Services (services.msc)" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "Press any key to exit..."
$null = $host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")