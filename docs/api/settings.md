# Settings API

Endpoints for reading and writing comprehensive device settings including performance configuration, fan curves, and device information.

## Get Settings

Retrieves the current device configuration including device info, performance mode, and fan curves.

```
GET /api/settings
```

### Response

```json
{
  "success": true,
  "data": {
    "device": {
      "model": "ROG Strix G16 G614JV",
      "biosVersion": "BIOS Version 1.25.3",
      "manufacturer": "ASUSTeK COMPUTER INC.",
      "type": 1,
      "cpuModel": "13th Gen Intel(R) Core(TM) i9-13980HX",
      "gpuModel": "NVIDIA GeForce RTX 4090",
      "supportedFeatures": ["Aura", "FanControl", "GPUModes", "PerformanceModes", "BatteryLimit"],
      "cpuFanCount": 1,
      "gpuFanCount": 1,
      "supportsAura": true,
      "supportsFanControl": true,
      "supportsGpuModes": true,
      "supportsBatteryLimit": true,
      "supportsPerformanceModes": true,
      "isAtkAcpiAvailable": true
    },
    "performanceMode": 1,
    "cpuFanCurve": [
      { "temperature": 40, "speed": 0 },
      { "temperature": 50, "speed": 25 },
      { "temperature": 60, "speed": 50 },
      { "temperature": 70, "speed": 75 },
      { "temperature": 80, "speed": 100 }
    ],
    "gpuFanCurve": [
      { "temperature": 40, "speed": 0 },
      { "temperature": 55, "speed": 30 },
      { "temperature": 65, "speed": 55 },
      { "temperature": 75, "speed": 80 },
      { "temperature": 85, "speed": 100 }
    ]
  }
}
```

### DeviceInfo Fields

| Field | Type | Description |
|-------|------|-------------|
| `model` | `string` | Hardware model name (e.g., "ROG Strix G16") |
| `biosVersion` | `string` | BIOS version string |
| `manufacturer` | `string` | Device manufacturer |
| `type` | `DeviceType` (int) | Device type (0=Unknown, 1=Laptop, 2=Desktop, 3=Ally, 4=Tablet) |
| `cpuModel` | `string` | Detected CPU model |
| `gpuModel` | `string` | Detected GPU model |
| `supportedFeatures` | `string[]` | List of supported feature names |
| `cpuFanCount` | `int` | Number of CPU fans detected |
| `gpuFanCount` | `int` | Number of GPU fans detected |
| `supportsAura` | `bool` | Aura RGB support |
| `supportsFanControl` | `bool` | Fan curve control support |
| `supportsGpuModes` | `bool` | GPU mode switching support |
| `supportsBatteryLimit` | `bool` | Battery charge limiting support |
| `supportsPerformanceModes` | `bool` | Performance mode switching support |
| `isAtkAcpiAvailable` | `bool` | ATKACPI driver availability |

---

## Update Settings

Updates performance configuration including mode, fan curves, and power limits.

```
PUT /api/settings
```

### Request Body

```json
{
  "mode": 2,
  "cpuPowerLimit": 65,
  "gpuPowerLimit": 120,
  "cputempLimit": 95,
  "fanCpuMin": 0,
  "fanGpuMin": 0,
  "cpuFanCurve": [40, 0, 50, 25, 60, 50, 70, 75, 80, 100],
  "gpuFanCurve": [40, 0, 55, 30, 65, 55, 75, 80, 85, 100],
  "autoApplyFans": true,
  "autoApplyPower": true
}
```

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `mode` | `AsusMode` (int) | No | Performance mode (0-4) |
| `cpuPowerLimit` | `int` | No | CPU power limit in watts |
| `gpuPowerLimit` | `int` | No | GPU power limit in watts |
| `cputempLimit` | `int` | No | CPU temperature limit in °C |
| `fanCpuMin` | `int` | No | Minimum CPU fan speed % |
| `fanGpuMin` | `int` | No | Minimum GPU fan speed % |
| `cpuFanCurve` | `byte[]` | No | CPU fan curve as temperature/speed byte pairs |
| `gpuFanCurve` | `byte[]` | No | GPU fan curve as temperature/speed byte pairs |
| `autoApplyFans` | `bool` | No | Auto-apply fan settings |
| `autoApplyPower` | `bool` | No | Auto-apply power settings |

### Response

```json
{
  "success": true,
  "error": null
}
```

### Notes

- The API applies the mode change first, then fan curves, then power settings.
- Fan curve arrays use the same byte format as the [Performance API](performance.md#set-fan-curve).
- Only include fields you want to change; omitted fields retain their current values.

### Example

```bash
# Switch to Turbo mode with custom fan curves
curl -X PUT http://localhost:5000/api/settings \
  -H "Content-Type: application/json" \
  -d '{
    "mode": 2,
    "cpuFanCurve": [40, 0, 50, 25, 60, 50, 70, 75, 80, 100],
    "gpuFanCurve": [40, 0, 55, 30, 65, 55, 75, 80, 85, 100]
  }'
```

---

## Health Check

Basic health check endpoint for monitoring and load balancers.

```
GET /health
```

### Response

```json
{
  "status": "Healthy"
}
```

### Example

```bash
curl http://localhost:5000/health
```