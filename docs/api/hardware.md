# Hardware State API

Endpoints for reading current hardware state including CPU, GPU, battery, and fan data.

## Get Full Hardware State

Retrieves a complete snapshot of all hardware sensors.

```
GET /api/hardware/state
```

### Response

```json
{
  "success": true,
  "data": {
    "cpu": {
      "temperature": 65,
      "usage": 42,
      "power": 45,
      "clockMHz": 3200,
      "powerLimit": 65
    },
    "gpu": {
      "temperature": 58,
      "hotspotTemperature": 62,
      "usage": 78,
      "power": 120,
      "usedVramMB": 2048,
      "totalVramMB": 8192,
      "coreClockMHz": 1800,
      "memoryClockMHz": 5000
    },
    "battery": {
      "chargePercent": 75,
      "isCharging": true,
      "chargeLimit": 80,
      "status": "AC"
    },
    "fan": {
      "cpuFanSpeed": 45,
      "cpuFanRpm": 1800,
      "gpuFanSpeed": 60,
      "gpuFanRpm": 2400,
      "midFanSpeed": 35
    },
    "sensors": [
      { "name": "CPU Temperature", "value": 65, "unit": "°C", "type": "Temperature", "timestamp": "2025-01-15T10:30:00Z" }
    ],
    "timestamp": "2025-01-15T10:30:00Z"
  }
}
```

### HardwareState Fields

| Field | Type | Description |
|-------|------|-------------|
| `cpu` | `CpuState` | CPU sensor data |
| `gpu` | `GpuState` | GPU sensor data |
| `battery` | `BatteryState` | Battery sensor data |
| `fan` | `FanState` | Fan sensor data |
| `sensors` | `SensorReading[]` | Raw sensor readings |
| `timestamp` | `DateTime` | When the snapshot was collected |

### CpuState Fields

| Field | Type | Description |
|-------|------|-------------|
| `temperature` | `int` | CPU temperature in °C |
| `usage` | `int` | CPU utilization percentage (0-100) |
| `power` | `int` | CPU power draw in watts |
| `clockMHz` | `int` | Current CPU clock speed in MHz |
| `powerLimit` | `int` | Current CPU power limit in watts |

### GpuState Fields

| Field | Type | Description |
|-------|------|-------------|
| `temperature` | `int` | GPU temperature in °C |
| `hotspotTemperature` | `int` | GPU hotspot temperature in °C |
| `usage` | `int` | GPU utilization percentage (0-100) |
| `power` | `int` | GPU power draw in watts |
| `usedVramMB` | `long` | Used VRAM in MB |
| `totalVramMB` | `long` | Total VRAM in MB |
| `coreClockMHz` | `int` | GPU core clock in MHz |
| `memoryClockMHz` | `int` | GPU memory clock in MHz |

### BatteryState Fields

| Field | Type | Description |
|-------|------|-------------|
| `chargePercent` | `int` | Battery charge percentage (0-100) |
| `isCharging` | `bool` | Whether the device is charging |
| `chargeLimit` | `int` | Battery charge limit percentage |
| `status` | `string` | Battery status ("AC" or "DC") |

### FanState Fields

| Field | Type | Description |
|-------|------|-------------|
| `cpuFanSpeed` | `int` | CPU fan speed percentage (0-100) |
| `cpuFanRpm` | `int` | CPU fan RPM |
| `gpuFanSpeed` | `int` | GPU fan speed percentage (0-100) |
| `gpuFanRpm` | `int` | GPU fan RPM |
| `midFanSpeed` | `int` | Mid fan speed percentage (0-100) |

---

## Get CPU State Only

Retrieves only the CPU hardware state.

```
GET /api/hardware/cpu
```

### Response

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

---

## Get GPU State Only

Retrieves only the GPU hardware state.

```
GET /api/hardware/gpu
```

### Response

```json
{
  "success": true,
  "data": {
    "temperature": 58,
    "hotspotTemperature": 62,
    "usage": 78,
    "power": 120,
    "usedVramMB": 2048,
    "totalVramMB": 8192,
    "coreClockMHz": 1800,
    "memoryClockMHz": 5000
  }
}
```

---

## Get Battery State Only

Retrieves only the battery hardware state.

```
GET /api/hardware/battery
```

### Response

```json
{
  "success": true,
  "data": {
    "chargePercent": 75,
    "isCharging": true,
    "chargeLimit": 80,
    "status": "AC"
  }
}
```

---

## Get Fan State Only

Retrieves only the fan hardware state.

```
GET /api/hardware/fan
```

### Response

```json
{
  "success": true,
  "data": {
    "cpuFanSpeed": 45,
    "cpuFanRpm": 1800,
    "gpuFanSpeed": 60,
    "gpuFanRpm": 2400,
    "midFanSpeed": 35
  }
}
```

---

### Example: cURL

```bash
curl http://localhost:5000/api/hardware/state
```

### Example: Python

```python
import requests

response = requests.get("http://localhost:5000/api/hardware/state")
data = response.json()

if data["success"]:
    cpu_temp = data["data"]["cpu"]["temperature"]
    gpu_usage = data["data"]["gpu"]["usage"]
    print(f"CPU: {cpu_temp}°C, GPU: {gpu_usage}%")
```