$packageName = 'Papercut.Service'
$serviceName = "XSockets-4.0.0"
Write-Host "Installing Windows Service: $($packageName)"
# verify if the service already exists, and if yes remove it first
if (Get-Service $serviceName -ErrorAction SilentlyContinue)
{
  # using WMI to remove Windows service because PowerShell does not have CmdLet for this
    $serviceToRemove = Get-WmiObject -Class Win32_Service -Filter "name='$serviceName'"
    #Stop-Service $serviceName
    $serviceToRemove.delete()
    Write-Host "service $($packageName) removed"
}
else
{
  # just do nothing
    Write-Host "service $($packageName) does not exists"
}
 
Write-Host "installing $($packageName) service from $(Split-Path $MyInvocation.MyCommand.Path))"
# creating credentials which can be used to run my windows service
#$secpasswd = ConvertTo-SecureString "MyPa$$word" -AsPlainText -Force
#$mycreds = New-Object System.Management.Automation.PSCredential (".\MyUserName", $secpasswd)
$binaryPath = "$(Split-Path $MyInvocation.MyCommand.Path)\XSockets.Windows.Service.exe"
# creating windows service using all provided parameters
New-Service -name $serviceName -binaryPathName $binaryPath -displayName $serviceName -startupType Automatic 
#-credential $mycreds
#Start-Service $serviceName
Write-Host "$($packageName) installation completed"