# System Architecture

## Overview

ZTR_OS is a .NET 9.0-based hardware abstraction and AI optimization platform for ASUS ROG devices. The system is organized into modular layers with clear separation of concerns.

## Component Diagram

```
┌─────────────────────────────────────────────────────────────────┐
│                        Client Applications                      │
│  (Web UI, Mobile App, Third-Party Tools, CLI)                   │
└──────────────────────────┬──────────────────────────────────────┘
                           │ HTTP REST / SignalR
                           ▼
┌─────────────────────────────────────────────────────────────────┐
│                        ZTR.Api (ASP.NET Core)                    │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────────────┐   │
│  │ Controllers   │  │ SignalR Hubs │  │  Middleware           │   │
│  │ - Hardware    │  │ - Sensor     │  │  - Exception         │   │
│  │ - Performance │  │ - State      │  │  - Integration        │   │
│  │ - Aura        │  │ - Hardware   │  │  - Performance Log    │   │
│  │ - MLP         │  └──────────────┘  └──────────────────────┘   │
│  │ - Binding     │                                                │
│  │ - Settings    │                                                │
│  │ - Process     │                                                │
│  └──────┬───────┘                                                 │
└─────────┼────────────────────────────────────────────────────────┘
          │
          ▼
┌─────────────────────────────────────────────────────────────────┐
│                     ZTR.Intelligence                             │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────────────┐   │
│  │ MlpNetwork   │  │ DecisionEngine│  │  OnlineLearner       │   │
│  │ (3-layer NN) │  │ (Action Map) │  │  (Backprop)          │   │
│  └──────┬───────┘  └──────┬───────┘  └──────────┬───────────┘   │
│         │                  │                      │              │
│  ┌──────┴──────────────────┴──────────────────────┴───────────┐  │
│  │              SensorFeatureExtractor                         │  │
│  └──────────────────────────┬──────────────────────────────────┘  │
│                              │                                    │
│  ┌──────────────────────────┴──────────────────────────────────┐  │
│  │              PredictiveScheduler                              │  │
│  └──────────────────────────────────────────────────────────────┘  │
└──────────────────────────┬──────────────────────────────────────┘
                           │
                           ▼
┌─────────────────────────────────────────────────────────────────┐
│                       ZTR.HAL (Hardware Abstraction Layer)       │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────────────┐   │
│  │   AsusAcpi   │  │   AsusHid    │  │     WmiHelper        │   │
│  │  (ACPI IoC)  │  │  (HID I2C)  │  │    (WMI Queries)     │   │
│  └──────┬───────┘  └──────┬───────┘  └──────────┬───────────┘   │
│         │                  │                      │              │
│  ┌──────┴──────────────────┴──────────────────────┴───────────┐  │
│  │                    DeviceProbe / DeviceInfo                 │  │
│  └──────────────────────────────────────────────────────────────┘  │
│                                                                    │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────────────┐   │
│  │ ModeControl  │  │ AuraLighting │  │  PowerLimitManager   │   │
│  └──────────────┘  └──────────────┘  └──────────────────────┘   │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────────────┐   │
│  │CpuAffinityMgr│  │GpuAffinityMgr│  │  ProcessTracker      │   │
│  └──────────────┘  └──────────────┘  └──────────────────────┘   │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────────────┐   │
│  │ TopologySvc  │  │ FanCurveCalc │  │  SensorPipeline      │   │
│  └──────────────┘  └──────────────┘  └──────────────────────┘   │
│                                                                    │
│  ┌──────────────────────────────────────────────────────────────┐ │
│  │ GPU Control (IGpuControl)                                   │ │
│  │  ┌────────────┐  ┌────────────┐  ┌────────────────────┐     │ │
│  │  │NvidiaGpuCtrl│  │ AmdGpuCtrl │  │  Adl2Gpu / NvApi   │     │ │
│  │  └────────────┘  └────────────┘  └────────────────────┘     │ │
│  └──────────────────────────────────────────────────────────────┘ │
└──────────────────────────┬──────────────────────────────────────┘
                           │
                           ▼
┌─────────────────────────────────────────────────────────────────┐
│                     Hardware (ASUS ROG Device)                   │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────────────┐   │
│  │  ACPI 0xAB59 │  │  HID I2C    │  │  GPU (NVAPI/ADL2)    │   │
│  └──────────────┘  └──────────────┘  └──────────────────────┘   │
└─────────────────────────────────────────────────────────────────┘
```

## Module Descriptions

### ZTR.Models

Defines all data models, enums, DTOs, and API contracts shared across modules. This is the lowest-level dependency.

**Key files:**
- `HardwareState.cs` — CPU, GPU, Battery, Fan state models
- `MlpModel.cs` — MLP configuration, decisions, training samples
- `PerformanceConfig.cs` — Performance configuration, fan curve points
- `ProcessBinding.cs` — Process binding, affinity, topology models
- `DeviceInfo.cs` — Device detection and capability information
- `AsusDevice.cs` — All enumerations (modes, fans, Aura modes, device IDs)
- `ApiDtos.cs` — API request/response DTOs

### ZTR.HAL (Hardware Abstraction Layer)

Provides unified access to ASUS hardware through ACPI, HID, GPU APIs, and WMI. This is the core hardware interaction layer.

**Key components:**
- `AsusAcpi` — ACPI IoC communication for performance modes, fan curves, power limits
- `AsusHid` — HID I2C communication for Aura RGB, keyboard, battery
- `ModeControl` — Central performance mode and fan curve controller
- `AuraLighting` — RGB lighting control with 20+ effect modes
- `SensorPipeline` — Synchronous and timer-based sensor data collection
- `SensorAggregator` — Combines readings from multiple sources
- `SensorDegradationHandler` — Detects sensor failures and provides fallback values
- `SensorQueue` — Circular buffer of recent hardware states
- `ProcessTracker` — Monitors running processes with CPU/GPU affinity
- `CpuAffinityManager` / `GpuAffinityManager` — Process affinity management
- `TopologyService` — CPU NUMA topology and GPU topology detection

### ZTR.Intelligence

Implements the AI-driven optimization engine that analyzes hardware data and makes intelligent tuning decisions.

**Key components:**
- `MlpNetwork` — 3-layer fully-connected neural network (16→64→32→8)
- `SensorFeatureExtractor` — Normalizes sensor data into MLP input features
- `PerformanceDecisionEngine` — Maps MLP outputs to validated hardware actions
- `OnlineLearner` — Real-time backpropagation training
- `PredictiveScheduler` — Schedules MLP inference and training cycles
- `DecisionLogger` — Records decision history with confidence scores
- `ManualOverride` — Allows manual control to override AI decisions

### ZTR.Api

ASP.NET Core web API hosting the REST endpoints and SignalR real-time hubs.

**Key components:**
- 7 Controllers — Hardware, Performance, Aura, MLP, Binding, Process, Settings
- 3 SignalR Hubs — Sensor, State, HardwareData
- `SensorSignalRBridge` — Bridges sensor pipeline to SignalR for real-time push
- Middleware — Exception handling, integration logging, performance logging
- Swagger — Auto-generated API documentation in development

### ZTR.Service

Windows Service host that runs the API as a background service.

## Data Flow

### Sensor Reading Flow

```
1. SensorBackgroundService (timer tick)
      │
      ▼
2. SensorPipeline.CollectData()
      │
      ├── AsusAcpi.DeviceGet() → CPU temperature, power, fan RPM
      ├── IGpuControl methods → GPU temperature, usage, power, clocks, VRAM
      ├── BatteryControl → Battery charge, charging status
      │
      ▼
3. SensorAggregator.Aggregate(readings)
      │
      ▼
4. SensorQueue.Enqueue(state)
      │
      ├── Trigger StateEnqueued event
      │
      ▼
5. SensorSignalRBridge.OnStateEnqueued()
      │
      ▼
6. SignalR Hub.Clients.All.SendCoreAsync("HardwareUpdate", dto)
```

### MLP Decision Flow

```
1. SensorPipeline.CollectOnce()
      │
      ▼
2. SensorFeatureExtractor.Extract(state)
      │  → 16-dimensional normalized feature vector
      │
      ▼
3. MlpNetwork.Predict(features)
      │  → 8-dimensional action vector [0,1]
      │
      ▼
4. PerformanceDecisionEngine.Decide(decision)
      │  → Validated list of HardwareAction commands
      │
      ▼
5. Apply actions:
      ├── ModeControl.SetMode() / SetFanCurve() / SetPowerLimits()
      ├── CpuAffinityManager.SetAffinity()
      └── GpuAffinityManager.SetGpuAffinity()
      │
      ▼
6. DecisionLogger.Log(decision)
```

## Technology Stack

| Component | Technology |
|-----------|-----------|
| Framework | .NET 9.0 (ASP.NET Core) |
| Language | C# 13 |
| Serialization | System.Text.Json (camelCase) |
| Real-time | SignalR |
| API Docs | Swashbuckle (Swagger) |
| DI Container | Built-in ASP.NET Core |
| Logging | Microsoft.Extensions.Logging |
| GPU APIs | NVAPI (NVIDIA), ADL2 (AMD) |
| Hardware Comm | ACPI IoC, HID I2C |
| Frontend | React + TypeScript + Vite |
| Testing | xUnit |
| Windows Service | .NET Worker Service |

## Project Structure

```
ZTR_OS/
├── src/
│   ├── ZTR.Models/          # Data models, enums, DTOs
│   ├── ZTR.Common/          # Shared utilities and interfaces
│   ├── ZTR.HAL/             # Hardware Abstraction Layer
│   │   ├── AsusAcpi.cs      # ACPI communication
│   │   ├── AsusHid.cs       # HID I2C communication
│   │   ├── ModeControl.cs   # Performance mode controller
│   │   ├── AuraLighting.cs  # RGB lighting control
│   │   ├── SensorPipeline.cs # Sensor data collection
│   │   └── ...
│   ├── ZTR.Intelligence/   # MLP engine and AI optimization
│   │   ├── MlpNetwork.cs    # Neural network implementation
│   │   ├── PerformanceDecisionEngine.cs # Action mapping
│   │   ├── OnlineLearner.cs # Real-time training
│   │   └── ...
│   ├── ZTR.Api/             # ASP.NET Core REST + SignalR API
│   │   ├── Controllers/     # 7 REST controllers
│   │   ├── Hubs/            # 3 SignalR hubs
│   │   ├── Middleware/      # Request middleware
│   │   └── Program.cs       # App entry point
│   └── ZTR.Service/        # Windows Service host
├── tests/
│   ├── ZTR.HAL.Tests/       # HAL unit tests
│   ├── ZTR.Intelligence.Tests/ # Intelligence tests
│   └── ZTR.Api.Tests/       # API integration tests
├── frontend/                # React web UI
├── docs/                    # Documentation
├── examples/                # Client examples
└── ZTR_OS.sln
```