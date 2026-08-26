# Hardware Protocols Reference

This document details the communication protocols used to interact with ASUS ROG hardware.

## Overview

ZTR_OS communicates with ASUS devices through three primary channels:

1. **ACPI (Advanced Configuration and Power Interface)** — For performance modes, fan curves, power limits
2. **HID (Human Interface Device)** — For RGB lighting, keyboard, battery, and other peripherals
3. **GPU APIs (NVAPI/ADL2)** — For GPU-specific control and monitoring

These protocols are abstracted behind the HAL layer, providing a clean API for the rest of the system.

## ACPI Protocol

### Communication Method

ASUS devices expose an ACPI interface via the `ATKACPI` driver. Communication uses IoCTL (IO Control) calls with device method IDs encoded as 4-character codes (FourCC).

### Architecture

```
┌────────────┐    IoCTL     ┌──────────────┐    PCI/I2C    ┌──────────┐
│  ZTR_OS    │ ──────────►  │  ATKACPI     │ ──────────►  │   ASUS   │
│  (AsusAcpi)│              │   Driver     │              │  Device  │
└────────────┘              └──────────────┘              └──────────┘
```

### Device Methods

| Method ID | Code | Description |
|-----------|------|-------------|
| `0x53545344` | `DSTS` | Read device status |
| `0x53564544` | `DEVS` | Write device status |
| `0x54494E49` | `INIT` | Initialize device |
| `0x474F4457` | `WDOG` | Watchdog management |

### Device Control IDs

These are the specific control functions accessed through `DEVS`/`DSTS`:

#### Performance Mode

| ID | Name | Description |
|----|------|-------------|
| `0x00050021` | `PerformanceMode` | Get/set performance mode |
| `0x00050012` | `StatusMode` | Device status query |

#### Fan Control

| ID | Name | Description |
|----|------|-------------|
| `0x00050005` | `CPU_Fan` | CPU fan RPM/speed read |
| `0x00050006` | `GPU_Fan` | GPU fan RPM/speed read |
| `0x00050007` | `Mid_Fan` | Mid fan RPM/speed read |
| `0x00050040` | `DevsCPUFanCurve` | CPU fan curve read/write |
| `0x00050041` | `DevsGPUFanCurve` | GPU fan curve read/write |
| `0x00050042` | `DevsMidFanCurve` | Mid fan curve read/write |

#### Power Limits

| ID | Name | Description |
|----|------|-------------|
| `0x00050060` | `PPT_APUA0` | SPL (Short Power Limit) |
| `0x00050063` | `PPT_APUA3` | sPPT (Short Power Peak Throttling) |
| `0x00050071` | `PPT_APUC1` | fPPT (Fast Power Peak Throttling) |

#### GPU Modes

| ID | Name | Description |
|----|------|-------------|
| `0x00050050` | `GPUBase` | GPU base mode |
| `0x00050051` | `GPUEco` | GPU Eco mode toggle |
| `0x00050052` | `GPUPower` | GPU power mode |
| `0x00050058` | `GPUMux` | GPU MUX switch |

#### Battery & Charger

| ID | Name | Description |
|----|------|-------------|
| `0x00050024` | `BatteryLimit` | Battery charge limit |
| `0x00050025` | `BatteryDischarge` | Battery discharge control |
| `0x00050026` | `ChargerMode` | Charger mode selection |

#### Other Controls

| ID | Name | Description |
|----|------|-------------|
| `0x00050019` | `CPUBacklight` | CPU backlight control |
| `0x00050027` | `KeyboardLight` | Keyboard backlight |
| `0x00050028` | `FnLock` | Fn key lock |
| `0x00050029` | `TouchpadToggle` | Touchpad enable/disable |
| `0x0005002B` | `AudioMute` | Audio mute |
| `0x0005002C` | `MicMute` | Microphone mute |
| `0x0005002D` | `CameraShutter` | Camera shutter |
| `0x0005002E` | `CameraLed` | Camera LED |

#### Screen Control

| ID | Name | Description |
|----|------|-------------|
| `0x00050030` | `ScreenOverdrive` | Screen overdrive |
| `0x00050031` | `ScreenFHD` | Screen FHD mode |
| `0x00050032` | `ScreenMiniled1` | MiniLED preset 1 |
| `0x00050033` | `ScreenMiniled2` | MiniLED preset 2 |
| `0x00050034` | `ScreenOptimalBrightness` | Optimal brightness |

### Performance Modes

| Value | Name | Description |
|-------|------|-------------|
| 0 | `PerformanceSilent` | Minimum fan, lowest power |
| 1 | `PerformanceBalanced` | Balanced acoustics and performance |
| 2 | `PerformanceTurbo` | Maximum performance, active cooling |
| 3 | `PerformanceFullSpeed` | Full speed, no fan control |
| 4 | `PerformanceManual` | Custom manual configuration |

## HID Protocol (Aura RGB)

### Communication Method

Aura RGB lighting uses the HID (Human Interface Device) protocol over I2C. The `AsusHid` class sends feature reports to the ASUS device's HID endpoint.

### Aura Message Format

All Aura messages are 17 bytes:

```
Byte 0:   0x5D (Header)
Byte 1:   0xB3 (Aura command)
Byte 2:   Zone (0-5)
Byte 3:   Mode (0-27)
Byte 4:   Red (0-255)
Byte 5:   Green (0-255)
Byte 6:   Blue (0-255)
Byte 7:   Speed (0-255)
Byte 8:   Direction (0-3)
Byte 9:   Random (0-255)
Byte 10:  Red2 (0-255)
Byte 11:  Green2 (0-255)
Byte 12:  Blue2 (0-255)
Byte 13-16: 0x00 (Reserved)
```

### Aura Message Sequence

To apply a lighting change, three messages are sent in sequence:

1. **DATA** — `[0x5D, 0xB3, ...]` — The lighting mode and color data
2. **SET** — `[0x5D, 0xB5, 0x00, 0x00, 0x00]` — Commit the data
3. **APPLY** — `[0x5D, 0xB4]` — Apply to hardware

### Aura Zones

| Value | Name | Description |
|-------|------|-------------|
| 0 | `Keyboard` | Main keyboard backlight |
| 1 | `Touchpad` | Touchpad area |
| 2 | `Body` | Device body panels |
| 3 | `Rear` | Rear/under-glow strip |
| 4 | `Mouse` | Mouse (when connected) |
| 5 | `Monitor` | Built-in monitor |

### Aura Modes

The Aura system supports 20+ lighting modes. See the [Aura API documentation](../api/aura.md) for the complete list.

### Special Features

- **PerKey Mode**: 178 individually addressable LEDs on supported keyboards. Requires sending 534 bytes (178 × 3 RGB bytes) via `SetFeatureAura`.
- **4-Zone Mode**: Zones the keyboard into 4 independent regions.
- **Aura Sync**: Synchronizes lighting between keyboard and mouse via `[0x5D, 0xB6, 0x01/0x00, 0x00, 0x00]`.

## GPU Control Protocols

### NVIDIA (NVAPI)

For NVIDIA GPUs, ZTR_OS uses the NVAPI library through `NvApiGpu` and `NvidiaGpuControl`:

- **Temperature**: `NvAPI_GPU_GetThermalSettings()`
- **Usage**: `NvAPI_GPU_GetUsages()`
- **Power**: `NvAPI_GPU_GetPowerReaderData()`
- **Clocks**: `NvAPI_GPU_GetClockInfo()`
- **VRAM**: `NvAPI_GPU_GetMemoryInfo()`
- **Fan Speed**: `NvAPI_GPU_GetTachReading()`

The `INvApiGpu` interface provides a mockable abstraction for testing.

### AMD (ADL2)

For AMD GPUs, ZTR_OS uses the ADL2 library through `Adl2Gpu` and `AmdGpuControl`:

- **Temperature**: `ADL2_OverdriveAPI_GetTemperature()`
- **Usage**: `ADL2_OverdriveAPI_GetGPUUsage()`
- **Power**: `ADL2_OverdriveAPI_GetPower()`
- **Clocks**: `ADL2_OverdriveAPI_GetClock()`
- **VRAM**: `ADL2_OverdriveAPI_GetVramInfo()`

### GPU Mode Switching

GPU modes (Eco/Standard/Ultimate) are controlled via ACPI commands:

| Mode | Description |
|------|-------------|
| `GPUEco` (0x00050051) | Power-saving mode, dGPU may be off |
| `GPUBase` (0x00050050) | Standard performance |
| `GPUPower` (0x00050052) | High-performance mode |
| `GPUMux` (0x00050058) | GPU MUX switching (dGPU only) |

## Fan Curve Protocol

### Fan Curve Byte Format

Fan curves are encoded as arrays of byte pairs: `[temp1, speed1, temp2, speed2, ...]`.

Each curve must have exactly `FanCurveCalculator.CurveByteSize` bytes (typically 20 bytes for 10 temperature/speed pairs).

### Fan Curve Calculation

The `FanCurveCalculator` class provides:

- `CurveToBytes(FanCurvePoint[])` — Converts point array to byte array
- `BytesToCurve(byte[])` — Converts byte array back to points
- `CalculateDefaultCurve(AsusFan, AsusMode)` — Generates default curves per mode

### Temperature Ranges

| Fan Type | Min °C | Max °C | Description |
|----------|--------|--------|-------------|
| CPU | 40 | 100 | CPU temperature trigger range |
| GPU | 40 | 95 | GPU temperature trigger range |
| Mid | 45 | 100 | Mid fan (chipset/VRM) trigger range |

## WMI Protocol

Windows Management Instrumentation (WMI) is used for:

- System information (manufacturer, model, BIOS)
- CPU details (cores, threads, topology)
- Battery status fallback
- OS information

The `WmiHelper` class provides a wrapper with caching.

## Device Detection Flow

```
1. DeviceProbe.Probe()
      │
      ├── Check ATKACPI driver availability
      │     └── Try AsusAcpi.INIT → success = driver available
      │
      ├── Read device info via WMI
      │     ├── Win32_ComputerSystem → Manufacturer, Model
      │     ├── Win32_BIOS → BIOS version
      │     ├── Win32_Processor → CPU model
      │
      ├── Check GPU availability
      │     ├── Try NVAPI → NVIDIA GPU detected
      │     └── Try ADL2 → AMD GPU detected
      │
      └── Compile supported features list
            ├── Has AsusAcpi → FanControl, PerformanceModes
            ├── Has AsusHid → Aura, KeyboardLight
            ├── Has GPU → GPUModes
            └── Has battery → BatteryLimit
```

## References

- [G-Helper](https://github.com/JustArchiNET/ASUS-GPU-Switcher) — Reference implementation for many ACPI/HID protocols
- [NVAPI Documentation](https://developer.nvidia.com/nvapi) — NVIDIA GPU API
- [AMD ADL2 API](https://github.com/GPUOpen-LibrariesAndSDKs/display-library) — AMD GPU API
- [ACPI Specification](https://uefi.org/specifications) — Official ACPI standard