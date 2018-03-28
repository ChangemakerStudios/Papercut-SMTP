

$ErrorActionPreference = "Stop"

if(Test-Path '.\release\Papercut.Service'){
    Remove-Item -Recurse -Force .\release\Papercut.Service
}

dotnet publish src\Papercut.Service -c Release -o ..\..\release\Papercut.Service
Copy-Item .\Docker\Windows.Dockerfile -Destination .\release\Papercut.Service\Dockerfile

$time = (Get-Date).ToUniversalTime().ToString("yyyyMMddHHmmss")
docker build .\release\Papercut.Service --tag papercut:nanoserver-$time