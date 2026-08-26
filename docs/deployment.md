# Deployment Guide

This guide covers deploying ZTR_OS as a Windows Service for production use.

## Prerequisites

- Windows 10/11 (x64) or Windows Server 2019+
- .NET 9.0 Runtime (Windows Server Hosting Bundle)
- ASUS ROG device with ATKACPI driver installed
- Administrator privileges for service installation

## Publishing the Application

### Self-Contained Publish

Creates a single-folder deployment with the .NET runtime included:

```bash
cd /workspace/ZTR_OS
dotnet publish src/ZTR.Api -c Release -o ./publish/api --self-contained true -r win-x64
```

### Framework-Dependent Publish

Requires .NET 9.0 Runtime on the target machine:

```bash
cd /workspace/ZTR_OS
dotnet publish src/ZTR.Api -c Release -o ./publish/api -r win-x64
```

### Windows Service Publish

The `ZTR.Service` project is designed to run as a Windows Service:

```bash
cd /workspace/ZTR_OS
dotnet publish src/ZTR.Service -c Release -o ./publish/service --self-contained true -r win-x64
```

## Installing as a Windows Service

### Using the Provided Script

```powershell
# Run as Administrator
cd C:\path\to\ZTR_OS\src\ZTR.Service

# Install the service
.\install-service.ps1 -ServiceName "ZTR_OS" -DisplayName "ZTR_OS Backend" -Description "ZTR_OS Hardware Control and AI Optimization Service"
```

### Manual Installation

```powershell
# Create the service
sc.exe create ZTR_OS binPath= "C:\path\to\publish\service\ZTR.Service.exe" start= auto DisplayName= "ZTR_OS Backend"

# Configure service description
sc.exe description ZTR_OS "ZTR_OS Hardware Control and AI Optimization Service for ASUS ROG Devices"

# Set to auto-start
sc.exe config ZTR_OS start= auto
```

### Service Management

```powershell
# Start the service
sc.exe start ZTR_OS

# Stop the service
sc.exe stop ZTR_OS

# Query service status
sc.exe query ZTR_OS

# Uninstall the service
.\uninstall-service.ps1 -ServiceName "ZTR_OS"
# Or manually:
sc.exe delete ZTR_OS
```

## Configuration

### appsettings.json

The API configuration is in `appsettings.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "Kestrel": {
    "Limits": {
      "MaxConcurrentConnections": 100,
      "MaxRequestBodySize": 1048576
    }
  },
  "Urls": "http://localhost:5000"
}
```

### Environment-Specific Config

Create `appsettings.Production.json` for production overrides:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "Microsoft.AspNetCore": "Error"
    }
  },
  "AllowedHosts": "your-domain.com"
}
```

### Custom Port

Edit the `Urls` setting to change the listening port:

```json
{
  "Urls": "http://localhost:8080"
}
```

### HTTPS Configuration

For production with HTTPS:

```json
{
  "Urls": "https://localhost:5001;http://localhost:5000"
}
```

And configure a certificate:

```powershell
# Generate self-signed certificate (for testing)
dotnet dev-certs https --trust

# Or configure in Program.cs:
builder.WebHost.UseKestrel(options =>
{
    options.Listen(5001, listenOptions =>
    {
        listenOptions.UseHttps("path/to/certificate.pfx", "password");
    });
});
```

## Firewall Configuration

Allow inbound traffic on the configured port:

```powershell
# Open port 5000
New-NetFirewallRule -DisplayName "ZTR_OS API" -Direction Inbound -Protocol TCP -LocalPort 5000 -Action Allow
```

## Setting Up as a Daemon (Alternative)

Instead of a Windows Service, you can run ZTR_OS as a background process:

### Using NSSM (Non-Sucking Service Manager)

```bash
nssm install ZTR_OS "C:\path\to\publish\service\ZTR.Service.exe"
nssm set ZTR_OS AppDirectory "C:\path\to\publish\service"
nssm set ZTR_OS DisplayName "ZTR_OS Backend"
nssm set ZTR_OS Description "Hardware Control and AI Optimization Service"
nssm start ZTR_OS
```

### Using Windows Task Scheduler

1. Create a basic task with trigger "At startup"
2. Action: Start a program
3. Program: `C:\path\to\publish\service\ZTR.Service.exe`
4. Check "Run with highest privileges"

## Docker Deployment

### Dockerfile

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:9.0-nanoserver-latest AS base
WORKDIR /app
EXPOSE 5000

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["src/ZTR.Api/ZTR.Api.csproj", "src/ZTR.Api/"]
RUN dotnet restore "src/ZTR.Api/ZTR.Api.csproj"
COPY . .
RUN dotnet publish "src/ZTR.Api/ZTR.Api.csproj" -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "ZTR.Api.dll"]
```

### Build and Run

```bash
# Build
docker build -t ztr-os-api .

# Run
docker run -d -p 5000:5000 --name ztr-os ztr-os-api

# With volume for persistent data
docker run -d -p 5000:5000 -v ztr-data:/app/data --name ztr-os ztr-os-api
```

Note: Docker deployments have limited hardware access. The HAL may not be able to communicate with ACPI/HID devices in containerized environments.

## Health Check Verification

After deployment, verify the service is running:

```bash
# Health check
curl http://localhost:5000/health

# Full state check
curl http://localhost:5000/api/hardware/state

# SignalR connection test
# Use a SignalR client to connect to http://localhost:5000/hubs/sensor
```

## Troubleshooting

### Service Won't Start

```powershell
# Check event log
Get-EventLog -LogName Application -Source "ZTR_OS" -Newest 20

# Run directly to see error output
cd C:\path\to\publish\service
.\ZTR.Service.exe
```

### Hardware Not Detected

```powershell
# Verify ATKACPI driver
Get-PnpDevice -Class "ASUS" | Format-Table

# Check for HID device
Get-PnpDevice -Class "HID" | Where-Object { $_.FriendlyName -like "*ASUS*" }

# Run ZTR_OS and check /api/settings for device info
```

### Permission Issues

```powershell
# Ensure service runs with appropriate privileges
sc.exe qc ZTR_OS

# If needed, change the service account
sc.exe config ZTR_OS obj= "LocalSystem"
```

## Production Checklist

- [ ] Run as Windows Service (not console)
- [ ] Configure production logging level (Warning/Error)
- [ ] Set up firewall rules for the API port
- [ ] Enable HTTPS with a valid certificate
- [ ] Configure health check monitoring
- [ ] Set up log rotation or external logging (Serilog, ELK, etc.)
- [ ] Verify hardware access under the service account
- [ ] Test SignalR connectivity through any reverse proxy
- [ ] Document backup and restore procedures
- [ ] Set up alerting for service failures