param(
    [string]$ServiceName = "ZTR_OS"
)

$ErrorActionPreference = "Stop"

$service = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
if (-not $service) {
    Write-Host "Service '$ServiceName' is not installed."
    exit 0
}

if ($service.Status -ne "Stopped") {
    Write-Host "Stopping service '$ServiceName'..."
    Stop-Service -Name $ServiceName -Force
    Start-Sleep -Seconds 2
}

Write-Host "Uninstalling service '$ServiceName'..."
sc.exe delete $ServiceName | Out-Null

Write-Host "Service '$ServiceName' uninstalled successfully."