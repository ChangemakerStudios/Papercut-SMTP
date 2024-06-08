param (
    [string]$BuildVersion
)

if (-not $BuildVersion) {
    Write-Host "You must specify a build version. E.g.: 7.0.1"
    exit 1
}

$BuildDate = (Get-Date).ToString("yyyy-MM-ddTHH:mm:ssZ")
$VcsRef = (git rev-parse --short HEAD)

docker build -t "changemakerstudiosus/papercut-smtp:$BuildVersion" . `
    --build-arg BUILD_VERSION=$BuildVersion `
    --build-arg BUILD_DATE=$BuildDate `
    --build-arg VCS_REF=$VcsRef `
    --no-cache