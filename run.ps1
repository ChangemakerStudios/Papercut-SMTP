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
#
# The service tab builds with SkipAngularBuild: both tabs start at once, and
# letting MSBuild run an Angular build while ng serve is starting puts two
# Angular processes on the same .angular/cache, where they stall each other.
# Nothing is lost -- the UI you are iterating on comes from :4200 anyway. The
# embedded copy is built once below if it is missing, and refreshed by a plain
# 'dotnet build' whenever you actually want :8080 up to date.
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

# The service embeds Web\Assets at build time and fails if they are missing, so
# make sure they exist once -- after that the dev loop never rebuilds them.
if (-not (Test-Path (Join-Path $webDir 'Assets\index.html'))) {
    Write-Host 'Web assets missing - building the Angular UI once...' -ForegroundColor Yellow
    Push-Location $webDir
    npm run build
    Pop-Location
}

$serviceCmd = "dotnet run --no-launch-profile --property:SkipAngularBuild=true"
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
Write-Host ''
Write-Host '  The service tab skips the Angular build -- iterate on :4200.' -ForegroundColor DarkGray
Write-Host "  Run 'dotnet build' in src/Papercut.Service to refresh the copy embedded at :8080." -ForegroundColor DarkGray
