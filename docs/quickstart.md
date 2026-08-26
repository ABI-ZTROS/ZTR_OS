# Quick Start Guide

Get up and running with ZTR_OS in minutes.

## Prerequisites

| Requirement | Version |
|-------------|---------|
| Operating System | Windows 10/11 (x64) or Windows Server 2019+ |
| .NET Runtime | 9.0 SDK or later |
| ASUS Device | ROG laptop/desktop/Ally with ATKACPI driver |
| GPU (optional) | NVIDIA (NVAPI) or AMD (ADL2) for GPU monitoring |

### Checking Prerequisites

```powershell
# Check .NET version
dotnet --version

# Check if ATKACPI is available
Get-PnpDevice -Class "ASUS" | Format-Table

# Check GPU
nvidia-smi  # NVIDIA
# or
amd-smi     # AMD
```

## Installation

### Option 1: Run from Source

```bash
# Clone the repository
git clone <repository-url>
cd ZTR_OS

# Restore dependencies
dotnet restore

# Build
dotnet build

# Run the API
dotnet run --project src/ZTR.Api
```

### Option 2: Publish and Run

```bash
# Publish as self-contained
dotnet publish src/ZTR.Api -c Release -o ./publish --self-contained true -r win-x64

# Run
cd publish
./ZTR.Api.exe
```

### Option 3: Windows Service (Production)

```bash
# Publish the service
dotnet publish src/ZTR.Service -c Release -o ./publish/service --self-contained true -r win-x64

# Install as Windows Service (PowerShell as Administrator)
cd src/ZTR.Service
.\install-service.ps1 -ServiceName "ZTR_OS" -DisplayName "ZTR_OS Backend"

# Or manually:
sc.exe create ZTR_OS binPath= "C:\path\to\publish\service\ZTR.Service.exe" start= auto
sc.exe start ZTR_OS
```

See the [Deployment Guide](deployment.md) for detailed production setup.

## Verify the Installation

```bash
# Health check
curl http://localhost:5000/health

# Get hardware state
curl http://localhost:5000/api/hardware/state

# Get device info
curl http://localhost:5000/api/settings
```

If successful, you'll see JSON responses with your hardware data.

## Your First API Call

### Read CPU Temperature

```bash
curl http://localhost:5000/api/hardware/cpu
```

Response:
```json
{
  "success": true,
  "data": {
    "temperature": 65,
    "usage": 42,
    "power": 45,
    "clockMHz": 3200,
    "powerLimit": 65
  }
}
```

### Switch to Turbo Mode

```bash
curl -X POST http://localhost:5000/api/performance/mode \
  -H "Content-Type: application/json" \
  -d '{"mode": 2}'
```

### Set Static Red Aura on Keyboard

```bash
curl -X POST http://localhost:5000/api/aura/apply \
  -H "Content-Type: application/json" \
  -d '{"mode": 0, "zone": 0, "r": 255, "g": 0, "b": 0}'
```

### Monitor Hardware with Python

```python
import requests

while True:
    r = requests.get("http://localhost:5000/api/hardware/state")
    data = r.json()["data"]
    print(f"CPU: {data['cpu']['temperature']}°C | GPU: {data['gpu']['usage']}% | Batt: {data['battery']['chargePercent']}%")
```

## Connecting to SignalR

SignalR provides real-time hardware updates pushed to your client.

### JavaScript / TypeScript

```bash
npm install @microsoft/signalr
```

```javascript
import * as signalR from '@microsoft/signalr';

const connection = new signalR.HubConnectionBuilder()
  .withUrl('http://localhost:5000/hubs/sensor')
  .withAutomaticReconnect()
  .build();

connection.on('SensorUpdate', (state) => {
  console.log(`CPU: ${state.cpu.temperature}°C, GPU: ${state.gpu.usage}%`);
});

await connection.start();
```

### .NET Client

```bash
dotnet add package Microsoft.AspNetCore.SignalR.Client
```

```csharp
using Microsoft.AspNetCore.SignalR;

var connection = new HubConnectionBuilder()
    .WithUrl("http://localhost:5000/hubs/sensor")
    .Build();

connection.On<HardwareState>("SensorUpdate", state =>
{
    Console.WriteLine($"CPU: {state.Cpu.Temperature}°C, GPU: {state.Gpu.Usage}%");
});

await connection.StartAsync();
```

### SignalR Hub Endpoints

| Hub URL | Event | Description |
|---------|-------|-------------|
| `/hubs/sensor` | `SensorUpdate` | ~1s hardware state updates |
| `/hubs/state` | `StateChange` | Generic state changes |
| `/hubs/hardware` | `HardwareUpdate` | Group-based hardware data |

## Common Tasks

### Task: Create a Custom Fan Curve

```bash
# 1. See current curves
curl http://localhost:5000/api/performance/fan-curves

# 2. Set a custom CPU fan curve
# Format: [temp1, speed1, temp2, speed2, ...]
curl -X POST http://localhost:5000/api/performance/fan-curves \
  -H "Content-Type: application/json" \
  -d '{"device": 0, "curve": [40, 0, 50, 25, 60, 50, 70, 75, 80, 100]}'
```

### Task: Set Power Limits

```bash
# Set SPL=65W, sPPT=75W, fPPT=85W
curl -X POST http://localhost:5000/api/performance/power-limits \
  -H "Content-Type: application/json" \
  -d '{"spl": 65, "sppt": 75, "fppt": 85}'
```

### Task: Monitor AI Decisions

```bash
# Get last 10 MLP decisions
curl "http://localhost:5000/api/mlp/decisions?count=10"
```

### Task: Disable MLP Auto Mode Switching

```bash
curl -X PUT http://localhost:5000/api/mlp/config \
  -H "Content-Type: application/json" \
  -d '{"config": {"autoModeSwitch": false}}'
```

### Task: Find GPU-Intensive Processes

```bash
curl http://localhost:5000/api/process/gpu-intensive
```

### Task: Check System Topology

```bash
curl http://localhost:5000/api/binding/topology
```

## Using Postman

Import the Postman collection for easy API testing:

1. Open Postman
2. Import `examples/postman/ZTR_OS.postman_collection.json`
3. Set the `baseUrl` variable to your ZTR_OS API URL
4. Test any endpoint

## Next Steps

| Topic | Link |
|-------|------|
| Full API Reference | [API Documentation](api/README.md) |
| System Architecture | [Architecture Overview](architecture.md) |
| Hardware Protocols | [Protocols Reference](hardware-protocols.md) |
| MLP Engine Details | [MLP Engine Architecture](mlp-engine.md) |
| Deploying to Production | [Deployment Guide](deployment.md) |
| Extending the Platform | [Developer Guide](extending.md) |
| JavaScript Client | [JS Client](../examples/javascript-client/README.md) |
| Python Client | [Python Client](../examples/python-client/README.md) |

## Troubleshooting

### API Not Responding

```bash
# Check if service is running
# Windows Service:
sc.exe query ZTR_OS

# Or try running directly:
dotnet run --project src/ZTR.Api
```

### Hardware Not Detected

```powershell
# Verify ASUS drivers
Get-PnpDevice -Class "ASUS"

# The API returns device info at /api/settings
# Check isAtkAcpiSupported and supports* fields
```

### Swagger Not Available

Swagger is only available in Development mode (`ASPNETCORE_ENVIRONMENT=Development`). In production, access API docs directly from `docs/api/`.

### Permission Issues

If running as a Windows Service, ensure the service account has:
- Read access to hardware devices
- Administrator privileges for ACPI/HID communication
- Network access for SignalR connections

## Getting Help

- File an issue on GitHub
- Read the [full API reference](api/README.md)
- Check the [architecture documentation](architecture.md)