# Performance API

Endpoints for controlling performance modes, fan curves, and power limits.

## Get Current Performance Mode

Retrieves the current ASUS performance mode.

```
GET /api/performance/mode
```

### Response

```json
{
  "success": true,
  "data": 1
}
```

### AsusMode Enum Values

| Value | Name | Description |
|-------|------|-------------|
| 0 | `PerformanceSilent` | Silent mode for minimum fan noise |
| 1 | `PerformanceBalanced` | Balanced performance and acoustics |
| 2 | `PerformanceTurbo` | Turbo mode for maximum performance |
| 3 | `PerformanceFullSpeed` | Full speed mode for benchmarking |
| 4 | `PerformanceManual` | Custom manual configuration |

---

## Set Performance Mode

Switches to a different performance mode and applies associated fan curves and power settings.

```
POST /api/performance/mode
```

### Request Body

```json
{
  "mode": 2
}
```

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `mode` | `AsusMode` (int) | Yes | Target performance mode (0-4) |

### Response

**Success (200):**
```json
{
  "success": true,
  "error": null
}
```

**Failure (400):**
```json
{
  "success": false,
  "error": "Failed to set performance mode to 2"
}
```

### Example

```bash
# Switch to Turbo mode
curl -X POST http://localhost:5000/api/performance/mode \
  -H "Content-Type: application/json" \
  -d '{"mode": 2}'
```

---

## Get Fan Curves

Retrieves the current fan curve configuration for CPU, GPU, and Mid fans.

```
GET /api/performance/fan-curves
```

### Response

```json
{
  "success": true,
  "data": {
    "cpu": [
      { "temperature": 40, "speed": 0 },
      { "temperature": 50, "speed": 25 },
      { "temperature": 60, "speed": 50 },
      { "temperature": 70, "speed": 75 },
      { "temperature": 80, "speed": 100 }
    ],
    "gpu": [
      { "temperature": 40, "speed": 0 },
      { "temperature": 55, "speed": 30 },
      { "temperature": 65, "speed": 55 },
      { "temperature": 75, "speed": 80 },
      { "temperature": 85, "speed": 100 }
    ],
    "mid": [
      { "temperature": 45, "speed": 0 },
      { "temperature": 55, "speed": 20 },
      { "temperature": 65, "speed": 45 },
      { "temperature": 75, "speed": 70 },
      { "temperature": 85, "speed": 100 }
    ]
  }
}
```

Each fan curve is an array of `FanCurvePoint` objects:

| Field | Type | Description |
|-------|------|-------------|
| `temperature` | `int` | Temperature threshold in °C |
| `speed` | `int` | Fan speed percentage (0-100) |

---

## Set Fan Curve

Sets a custom fan curve for a specific device. The curve is sent as a byte array encoding temperature/speed pairs.

```
POST /api/performance/fan-curves
```

### Request Body

```json
{
  "device": 0,
  "curve": [40, 0, 50, 25, 60, 50, 70, 75, 80, 100]
}
```

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `device` | `AsusFan` (int) | Yes | Fan device (0=CPU, 1=GPU, 2=Mid, 3=XGM) |
| `curve` | `int[]` (byte array) | Yes | Alternating temperature/speed points as bytes |

### AsusFan Enum Values

| Value | Name |
|-------|------|
| 0 | `CPU` |
| 1 | `GPU` |
| 2 | `Mid` |
| 3 | `XGM` |

### Curve Byte Format

The curve is encoded as a flat byte sequence of `[temp1, speed1, temp2, speed2, ...]`. Each temperature/speed pair is one byte.

### Response

**Success (200):**
```json
{
  "success": true,
  "error": null
}
```

**Failure (400):**
```json
{
  "success": false,
  "error": "Failed to set fan curve for 0"
}
```

### Example

```bash
# Set CPU fan curve: 40°C→0%, 50°C→25%, 60°C→50%, 70°C→75%, 80°C→100%
curl -X POST http://localhost:5000/api/performance/fan-curves \
  -H "Content-Type: application/json" \
  -d '{"device": 0, "curve": [40, 0, 50, 25, 60, 50, 70, 75, 80, 100]}'
```

---

## Set Power Limits

Sets the CPU power limits: SPL, sPPT, and fPPT.

```
POST /api/performance/power-limits
```

### Request Body

```json
{
  "spl": 65,
  "sppt": 75,
  "fppt": 85
}
```

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `spl` | `int` | Yes | Short Power Limit in watts |
| `sppt` | `int` | Yes | Short Power Peak Throttling in watts |
| `fppt` | `int` | Yes | Fast Power Peak Throttling in watts |

### Power Limit Explained

| Acronym | Full Name | Description |
|---------|-----------|-------------|
| SPL | Short Power Limit | The sustained power draw limit |
| sPPT | Short Power Peak Throttling | Short-term peak power allowance above SPL |
| fPPT | Fast Power Peak Throttling | Very short-term (instant) power spike allowance |

### Response

**Success (200):**
```json
{
  "success": true,
  "error": null
}
```

**Failure (400):**
```json
{
  "success": false,
  "error": "Failed to set power limits"
}
```

### Example

```bash
# Set CPU power limits: SPL=65W, sPPT=75W, fPPT=85W
curl -X POST http://localhost:5000/api/performance/power-limits \
  -H "Content-Type: application/json" \
  -d '{"spl": 65, "sppt": 75, "fppt": 85}'
```