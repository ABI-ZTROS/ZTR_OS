# Aura RGB Lighting API

Endpoints for controlling ASUS Aura RGB lighting across supported devices.

## List Available Lighting Modes

Retrieves all available Aura lighting modes.

```
GET /api/aura/modes
```

### Response

```json
{
  "success": true,
  "data": [
    "Static",
    "Breathe",
    "ColorCycle",
    "Rainbow",
    "Star",
    "Rain",
    "Highlight",
    "Laser",
    "Ripple",
    "Strobe",
    "Comet",
    "Flash",
    "Heatmap",
    "GPUMode",
    "Ambient",
    "Battery",
    "Gradient",
    "ZoneTest",
    "Audio",
    "AudioPulse"
  ]
}
```

### AuraMode Enum Values

| Value | Name | Description |
|-------|------|-------------|
| 0 | `Static` | Solid static color |
| 1 | `Breathe` | Breathing fade in/out |
| 2 | `ColorCycle` | Cycles through the color wheel |
| 3 | `Rainbow` | Flowing rainbow effect |
| 4 | `Star` | Starry random twinkling |
| 5 | `Rain` | Raindrop-like animation |
| 6 | `Highlight` | Cursor position highlight |
| 7 | `Laser` | Laser beam effect |
| 8 | `Ripple` | Ripple expanding from point |
| 10 | `Strobe` | Strobing effect |
| 11 | `Comet` | Comet tail effect |
| 12 | `Flash` | Random flashing effect |
| 20 | `Heatmap` | Temperature-based color mapping |
| 21 | `GPUMode` | GPU usage-reactive lighting |
| 22 | `Ambient` | Screen color-reactive lighting |
| 23 | `Battery` | Battery charge level color |
| 24 | `Gradient` | Gradient color effect |
| 25 | `ZoneTest` | Zone identification test |
| 26 | `Audio` | Audio spectrum visualization |
| 27 | `AudioPulse` | Audio pulse effect |

---

## Apply Aura Lighting Mode

Applies a lighting mode to a specific Aura zone.

```
POST /api/aura/apply
```

### Request Body

```json
{
  "mode": 0,
  "zone": 0,
  "r": 255,
  "g": 0,
  "b": 0
}
```

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `mode` | `AuraMode` (int) | Yes | Lighting mode (see table above) |
| `zone` | `AuraZone` (int) | Yes | Target zone (see below) |
| `r` | `byte` (0-255) | Yes | Red component |
| `g` | `byte` (0-255) | Yes | Green component |
| `b` | `byte` (0-255) | Yes | Blue component |

### AuraZone Enum Values

| Value | Name | Description |
|-------|------|-------------|
| 0 | `Keyboard` | Keyboard backlight |
| 1 | `Touchpad` | Touchpad illumination |
| 2 | `Body` | Device body lighting |
| 3 | `Rear` | Rear/under-glow lighting |
| 4 | `Mouse` | Mouse lighting |
| 5 | `Monitor` | Monitor lighting |

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
  "error": "Failed to apply Aura mode 0"
}
```

### Examples

**Static red on keyboard:**
```bash
curl -X POST http://localhost:5000/api/aura/apply \
  -H "Content-Type: application/json" \
  -d '{"mode": 0, "zone": 0, "r": 255, "g": 0, "b": 0}'
```

**Breathe effect, cyan on touchpad:**
```bash
curl -X POST http://localhost:5000/api/aura/apply \
  -H "Content-Type: application/json" \
  -d '{"mode": 1, "zone": 1, "r": 0, "g": 255, "b": 255}'
```

**Rainbow on body:**
```bash
curl -X POST http://localhost:5000/api/aura/apply \
  -H "Content-Type: application/json" \
  -d '{"mode": 3, "zone": 2, "r": 0, "g": 0, "b": 0}'
```

**GPUMode-reactive on rear:**
```bash
curl -X POST http://localhost:5000/api/aura/apply \
  -H "Content-Type: application/json" \
  -d '{"mode": 21, "zone": 3, "r": 0, "g": 0, "b": 0}'
```

---

## Notes

- The Aura API requires the ASUS HID device to be connected and the `AsusHid` service to be initialized.
- Some zones may not be available on all devices. Use the device info from [Settings API](settings.md) to check supported features.
- Per-key (178-LED) direct control is available through the `AuraLighting.SetDirectMode()` method in the HAL, but is not yet exposed through the REST API.
- The API sends HID feature reports to the ASUS device using the `AsusHid` class.