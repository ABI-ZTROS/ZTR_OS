# ZTR_OS

> Intelligent hardware control and AI-driven optimization platform for ASUS ROG devices.

ZTR_OS is a .NET 9.0-based operating system management platform that provides hardware abstraction, real-time monitoring, AI-driven performance optimization, and a comprehensive REST + SignalR API for ASUS ROG laptops, desktops, and handheld devices.

## Key Features

- **Hardware Abstraction Layer (HAL)** — Unified access to CPU, GPU, battery, fans, RGB lighting, and power management through ACPI, HID, NVAPI, and ADL2 protocols
- **AI Optimization Engine** — 3-layer Multi-Layer Perceptron neural network that autonomously tunes performance, fan curves, power limits, and process affinity in real time
- **Real-Time Monitoring** — ~1 second hardware state polling with SignalR push notifications to connected clients
- **Performance Control** — Five performance modes, custom fan curves, configurable power limits (SPL/sPPT/fPPT), and GPU mode switching
- **Aura RGB Control** — 20+ lighting modes across 6 independently addressable zones with per-key LED support
- **Process Binding** — CPU and GPU affinity management with automatic detection of games and GPU-intensive workloads
- **Comprehensive API** — Fully documented REST API with Swagger, SignalR real-time hubs, and client examples for JavaScript, Python, and Postman

## System Architecture

```
┌──────────────┐     HTTP/SignalR     ┌──────────────┐     Intelligence     ┌──────────────┐
│   Clients    │ ◄──────────────────► │  ZTR.Api     │ ◄─────────────────► │ ZTR.Intel.   │
│  (Web, CLI)  │                      │  (REST+RTH)  │                      │  (MLP Engine) │
└──────────────┘                      └──────┬───────┘                      └──────┬───────┘
                                             │                                        │
                                             ▼                                        ▼
                                      ┌──────────────┐                      ┌──────────────┐
                                      │  ZTR.HAL     │ ◄─────────────────── │  Sensor Pipe │
                                      │ (Hardware)   │                      │  (Polling)   │
                                      └──────┬───────┘                      └──────┬───────┘
                                             │                                        │
                                             ▼                                        ▼
                                      ┌──────────────────────────────────────────────┐
                                      │        ASUS ROG Hardware (ACPI/HID/GPU)       │
                                      └──────────────────────────────────────────────┘
```

See the [Architecture Documentation](docs/architecture.md) for detailed component diagrams.

## Project Structure

```
ZTR_OS/
├── src/
│   ├── ZTR.Models/          # Data models, enums, DTOs
│   ├── ZTR.Common/          # Shared utilities and interfaces
│   ├── ZTR.HAL/             # Hardware Abstraction Layer
│   │   ├── AsusAcpi.cs      #   ACPI IoC communication
│   │   ├── AsusHid.cs       #   HID I2C communication
│   │   ├── ModeControl.cs   #   Performance mode controller
│   │   ├── AuraLighting.cs  #   RGB lighting control
│   │   ├── SensorPipeline.cs #  Sensor data collection
│   │   ├── ProcessTracker.cs #  Process monitoring
│   │   └── ...
│   ├── ZTR.Intelligence/    # MLP engine and AI optimization
│   │   ├── MlpNetwork.cs    #   3-layer neural network
│   │   ├── PerformanceDecisionEngine.cs # Action mapping
│   │   ├── OnlineLearner.cs #   Real-time backpropagation
│   │   └── ...
│   ├── ZTR.Api/             # ASP.NET Core REST + SignalR API
│   │   ├── Controllers/     #   7 REST controllers
│   │   ├── Hubs/            #   3 SignalR hubs
│   │   └── Program.cs       #   App entry point
│   └── ZTR.Service/         # Windows Service host
├── tests/
│   ├── ZTR.HAL.Tests/
│   ├── ZTR.Intelligence.Tests/
│   └── ZTR.Api.Tests/
├── frontend/                # React web UI
├── docs/                    # Documentation
├── examples/                # Client examples (JS, Python, Postman)
└── ZTR_OS.sln
```

## Technology Stack

| Component | Technology |
|-----------|-----------|
| Framework | .NET 9.0 (ASP.NET Core) |
| Language | C# 13 |
| Real-Time | SignalR |
| API Docs | Swashbuckle (Swagger) |
| GPU APIs | NVAPI (NVIDIA), ADL2 (AMD) |
| Hardware Comm | ACPI IoC, HID I2C |
| Frontend | React + TypeScript + Vite |
| Testing | xUnit |
| Deployment | Windows Service, Docker |

## Prerequisites

- **Windows 10/11 (x64)** or **Windows Server 2019+**
- **.NET 9.0 SDK** or later
- **ASUS ROG device** with ATKACPI driver installed
- Administrator privileges for hardware access

## Quick Start

### 1. Clone and Build

```bash
git clone <repository-url>
cd ZTR_OS
dotnet restore
dotnet build
```

### 2. Run the API

```bash
dotnet run --project src/ZTR.Api
# API running at http://localhost:5000
```

### 3. Test the API

```bash
# Health check
curl http://localhost:5000/health

# Get hardware state
curl http://localhost:5000/api/hardware/state

# Switch to Turbo mode
curl -X POST http://localhost:5000/api/performance/mode \
  -H "Content-Type: application/json" \
  -d '{"mode": 2}'
```

### 4. Connect a Client

```bash
# Python
pip install requests
python examples/python-client/ztr_client.py

# JavaScript
cd examples/javascript-client
npm install @microsoft/signalr
node example.js
```

## API Endpoints Overview

| Category | Endpoint | Method | Description |
|----------|----------|--------|-------------|
| **Hardware** | `/api/hardware/state` | GET | Full hardware snapshot |
| | `/api/hardware/cpu` | GET | CPU state |
| | `/api/hardware/gpu` | GET | GPU state |
| | `/api/hardware/battery` | GET | Battery state |
| | `/api/hardware/fan` | GET | Fan state |
| **Performance** | `/api/performance/mode` | GET, POST | Get/set performance mode |
| | `/api/performance/fan-curves` | GET, POST | Get/set fan curves |
| | `/api/performance/power-limits` | POST | Set CPU power limits |
| **Aura** | `/api/aura/modes` | GET | List lighting modes |
| | `/api/aura/apply` | POST | Apply lighting mode |
| **MLP** | `/api/mlp/config` | GET, PUT | Get/update MLP config |
| | `/api/mlp/decisions` | GET | Get AI decision history |
| | `/api/mlp/status` | GET | Check MLP engine status |
| **Binding** | `/api/binding/processes` | GET | List tracked processes |
| | `/api/binding/cpu` | POST | Set CPU affinity |
| | `/api/binding/gpu` | POST | Set GPU affinity |
| | `/api/binding/topology` | GET | System topology |
| **Settings** | `/api/settings` | GET, PUT | Device settings CRUD |

[Complete API Reference](docs/api/README.md)

## SignalR Real-Time Hubs

| Hub URL | Events | Description |
|---------|--------|-------------|
| `/hubs/sensor` | `SensorUpdate` | ~1s hardware state updates |
| `/hubs/state` | `StateChange` | Generic state changes |
| `/hubs/hardware` | `HardwareUpdate` | Group-based hardware data |

## Documentation

| Document | Description |
|----------|-------------|
| [Quick Start](docs/quickstart.md) | Get up and running in minutes |
| [API Reference](docs/api/README.md) | Complete API documentation with examples |
| [Architecture](docs/architecture.md) | System architecture and component diagrams |
| [Hardware Protocols](docs/hardware-protocols.md) | ACPI, HID, GPU protocol reference |
| [MLP Engine](docs/mlp-engine.md) | Neural network architecture and configuration |
| [Deployment](docs/deployment.md) | Windows Service and Docker deployment |
| [Extending](docs/extending.md) | Guide for extending the platform |

## Client Examples

| Language | Location | Description |
|----------|----------|-------------|
| JavaScript | [examples/javascript-client/](examples/javascript-client/README.md) | REST + SignalR client |
| Python | [examples/python-client/](examples/python-client/README.md) | REST API client |
| Postman | [examples/postman/](examples/postman/ZTR_OS.postman_collection.json) | Postman collection |

## Configuration

### appsettings.json

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "Urls": "http://localhost:5000"
}
```

### MLP Configuration

The MLP engine can be configured at runtime via the API:

```bash
# Disable auto mode switching
curl -X PUT http://localhost:5000/api/mlp/config \
  -H "Content-Type: application/json" \
  -d '{"config": {"autoModeSwitch": false}}'
```

## Development

### Build

```bash
dotnet build
```

### Run Tests

```bash
dotnet test
```

### Run in Development

```bash
# With Swagger
ASPNETCORE_ENVIRONMENT=Development dotnet run --project src/ZTR.Api
# Open http://localhost:5000/swagger
```

## Deployment

### Windows Service

```powershell
# Publish
dotnet publish src/ZTR.Service -c Release -o ./publish --self-contained true -r win-x64

# Install
cd src/ZTR.Service
.\install-service.ps1 -ServiceName "ZTR_OS"
```

### Docker

```bash
docker build -t ztr-os-api .
docker run -d -p 5000:5000 ztr-os-api
```

[Detailed Deployment Guide](docs/deployment.md)

## Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Make your changes
4. Run tests (`dotnet test`)
5. Commit (`git commit -m 'feat: add amazing feature'`)
6. Push (`git push origin feature/amazing-feature`)
7. Open a Pull Request

## License

TBD