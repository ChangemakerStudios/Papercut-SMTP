# ============================================================================
# Papercut SMTP — dev environment launcher
#
# Opens two Windows Terminal tabs:
#   1. Papercut.Service  (API + SMTP on http://localhost:8080, SMTP :2525)
#   2. ng serve          (live-reload web UI on http://localhost:4200,
#                         proxying /api and /hubs to the service)
#
# Develop against http://localhost:4200 — changes to Web/src hot-reload.
# The embedded UI (served by the service itself on :8080) only updates on a
# full 'dotnet build', so use :4200 while iterating.
# ============================================================================

$root = $PSScriptRoot
$serviceDir = Join-Path $root 'src\Papercut.Service'
$webDir = Join-Path $root 'src\Papercut.Service\Web'

# First-run convenience: make sure npm packages exist before ng serve opens
if (-not (Test-Path (Join-Path $webDir 'node_modules'))) {
    Write-Host 'node_modules missing - running npm install first...' -ForegroundColor Yellow
    Push-Location $webDir
    npm install
    Pop-Location
}

$serviceCmd = "dotnet run --no-launch-profile"
$webCmd = "npm start"

if (Get-Command wt -ErrorAction SilentlyContinue) {
    # Windows Terminal: one window, two tabs
    wt -w 0 new-tab --title 'Papercut Service' -d $serviceDir pwsh -NoExit -Command $serviceCmd `; `
       new-tab --title 'Web UI (ng serve)' -d $webDir pwsh -NoExit -Command $webCmd
}
else {
    # Fallback: two plain PowerShell windows
    Start-Process pwsh -WorkingDirectory $serviceDir -ArgumentList '-NoExit', '-Command', $serviceCmd
    Start-Process pwsh -WorkingDirectory $webDir -ArgumentList '-NoExit', '-Command', $webCmd
}

Write-Host ''
Write-Host 'Papercut dev environment starting:' -ForegroundColor Cyan
Write-Host '  Service (API/SMTP):  http://localhost:8080   (SMTP on :2525 in dev)' -ForegroundColor Gray
Write-Host '  Web UI (live):       http://localhost:4200' -ForegroundColor Green
