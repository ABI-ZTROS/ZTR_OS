# ZTR_OS API Reference

## Overview

The ZTR_OS API is a RESTful web service with real-time push capabilities via SignalR. It provides programmatic control over ASUS ROG hardware including performance modes, fan curves, power limits, RGB lighting, process binding, and AI-driven optimization.

## Base URL

```
http://localhost:5000
```

When running as a Windows Service, the default URL is configured in `appsettings.json`.

## Authentication

The API currently runs without authentication in development mode. When deploying to production, configure ASP.NET Core authentication middleware. See [Deployment Guide](../deployment.md) for details.

## Content Type

All requests and responses use `application/json`. The API serializes using camelCase property names.

## Common Response Format

All API responses follow a consistent envelope:

```json
{
  "success": true,
  "data": { ... },
  "error": null
}
```

| Field | Type | Description |
|-------|------|-------------|
| `success` | `bool` | Indicates whether the request succeeded |
| `data` | `T` | The response payload (type varies by endpoint) |
| `error` | `string?` | Error message when `success` is `false` |

## SignalR Hubs

The API exposes three SignalR hubs for real-time updates:

| Hub URL | Purpose |
|---------|---------|
| `/hubs/sensor` | Pushes `HardwareState` updates to all connected clients |
| `/hubs/state` | Generic state change notifications |
| `/hubs/hardware` | Hardware data group-based messaging |

### SignalR Events

**Sensor Hub** (`/hubs/sensor`):
- `SensorUpdate` — Sent when a new hardware state is collected (every ~1 second)

**State Hub** (`/hubs/state`):
- `StateChange` — Sent with a type string and data payload for state changes

**Hardware Data Hub** (`/hubs/hardware`):
- `JoinGroup(groupName)` — Join a named group
- `LeaveGroup(groupName)` — Leave a named group

## API Endpoints

| Category | Endpoint | Method |
|----------|----------|--------|
| [Hardware](hardware.md) | `/api/hardware/state` | GET |
| | `/api/hardware/cpu` | GET |
| | `/api/hardware/gpu` | GET |
| | `/api/hardware/battery` | GET |
| | `/api/hardware/fan` | GET |
| [Performance](performance.md) | `/api/performance/mode` | GET, POST |
| | `/api/performance/fan-curves` | GET, POST |
| | `/api/performance/power-limits` | POST |
| [Aura](aura.md) | `/api/aura/modes` | GET |
| | `/api/aura/apply` | POST |
| [MLP](mlp.md) | `/api/mlp/config` | GET, PUT |
| | `/api/mlp/decisions` | GET |
| | `/api/mlp/status` | GET |
| [Binding](binding.md) | `/api/binding/processes` | GET |
| | `/api/binding/cpu` | POST |
| | `/api/binding/gpu` | POST |
| | `/api/binding/topology` | GET |
| [Process](binding.md#process-endpoints) | `/api/process` | GET |
| | `/api/process/foreground` | GET |
| | `/api/process/gpu-intensive` | GET |
| [Settings](settings.md) | `/api/settings` | GET, PUT |
| [Health] | `/health` | GET |

## Error Codes

| HTTP Status | Description |
|-------------|-------------|
| 200 OK | Request succeeded |
| 400 Bad Request | Invalid parameters or hardware operation failed |
| 404 Not Found | Requested resource not found (e.g., no foreground process) |
| 500 Internal Server Error | Unhandled server error |

## Quick Start

```bash
# Get current hardware state
curl http://localhost:5000/api/hardware/state

# Set performance mode to Turbo
curl -X POST http://localhost:5000/api/performance/mode \
  -H "Content-Type: application/json" \
  -d '{"mode": 2}'

# List all MLP decisions
curl http://localhost:5000/api/mlp/decisions?count=20
```

## Further Reading

- [Quick Start Guide](../quickstart.md)
- [Architecture Overview](../architecture.md)
- [Hardware Protocols](../hardware-protocols.md)
- [Deployment Guide](../deployment.md)
- [MLP Engine](../mlp-engine.md)
- [Extending the Platform](../extending.md)