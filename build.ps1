# $exePath = "$env:TEMP\wix311.exe"
# (New-Object Net.WebClient).DownloadFile('https://github.com/wixtoolset/wix3/releases/download/wix3111rtm/wix311.exe', $exePath)
# cmd /c start /wait "$exePath" /q

cd .\build
.\build.ps1