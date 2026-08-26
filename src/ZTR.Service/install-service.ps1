param(
    [string]$ServiceName = "ZTR_OS",
    [string]$DisplayName = "ZTR_OS Backend Service",
    [string]$Description = "ZTR_OS operating system management platform - API and intelligence service",
    [string]$ExePath = "$PSScriptRoot\..\publish\ZTR.Service.exe"
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path $ExePath)) {
    Write-Error "Service executable not found at: $ExePath"
    exit 1
}

$existingService = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
if ($existingService) {
    Write-Host "Service '$ServiceName' already exists. Stopping and removing..."
    Stop-Service -Name $ServiceName -Force
    sc.exe delete $ServiceName | Out-Null
    Start-Sleep -Seconds 2
}

New-Service -Name $ServiceName `
    -BinaryPathName $ExePath `
    -DisplayName $DisplayName `
    -Description $Description `
    -StartupType Automatic

Write-Host "Service '$ServiceName' installed successfully."
Write-Host "Starting service..."

Start-Service -Name $ServiceName

Write-Host "Service '$ServiceName' started."
Write-Host "Status: $((Get-Service -Name $ServiceName).Status)"