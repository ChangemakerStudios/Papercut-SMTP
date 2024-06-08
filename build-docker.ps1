param (
    [string]$BuildVersion
)

if (-not $BuildVersion) {
    Write-Host "You must specify a build version. E.g.: 7.0.1"
    exit 1
}

docker build -t "changemakerstudiosus/papercut-smtp:$BuildVersion" . --build-arg "BUILD_VERSION=7$BuildVersion" --no-cache