# ZTR_OS Python Client

A Python client library for the ZTR_OS REST API with type-safe data classes.

## Prerequisites

- Python 3.10+
- `requests` library

## Installation

```bash
pip install requests
```

## Quick Start

```python
from ztr_client import ZTROClient

client = ZTROClient("http://localhost:5000")

# Read hardware state
state = client.get_hardware_state()
print(f"CPU: {state.cpu.temperature}°C, GPU: {state.gpu.usage}%")

# Set performance mode
client.set_performance_mode(2)  # Turbo mode

# Monitor over time
import time
for _ in range(10):
    state = client.get_hardware_state()
    print(f"CPU Temp: {state.cpu.temperature}°C")
    time.sleep(1)
```

## Features

- Full type-safe data classes for all hardware state
- Automatic error handling with `ZTROSError`
- Context manager support (`with` statement)
- All API endpoints covered
- Clean Pythonic API

## API Reference

### Hardware State

| Method | Description |
|--------|-------------|
| `get_hardware_state()` | Get complete hardware snapshot |
| `get_cpu_state()` | Get CPU state only |
| `get_gpu_state()` | Get GPU state only |
| `get_battery_state()` | Get battery state only |
| `get_fan_state()` | Get fan state only |

### Performance

| Method | Description |
|--------|-------------|
| `get_performance_mode()` | Get current performance mode |
| `set_performance_mode(mode)` | Set performance mode (0-4) |
| `get_fan_curves()` | Get all fan curves |
| `set_fan_curve(device, curve)` | Set fan curve for a device |
| `set_power_limits(spl, sppt, fppt)` | Set CPU power limits |

### Aura RGB

| Method | Description |
|--------|-------------|
| `list_aura_modes()` | List all lighting mode names |
| `apply_aura(mode, zone, r, g, b)` | Apply lighting to a zone |

### MLP Engine

| Method | Description |
|--------|-------------|
| `get_mlp_config()` | Get MLP configuration |
| `update_mlp_config(config)` | Update MLP configuration |
| `get_mlp_decisions(count)` | Get recent AI decisions |
| `get_mlp_status()` | Check if MLP is enabled |

### Process Binding

| Method | Description |
|--------|-------------|
| `list_processes()` | List tracked processes |
| `set_cpu_affinity(pid, mask)` | Set CPU affinity for a process |
| `set_gpu_affinity(pid, idx)` | Set GPU affinity for a process |
| `get_topology()` | Get CPU/GPU topology |
| `get_foreground_process()` | Get current foreground process |
| `get_gpu_intensive_processes()` | Get GPU-intensive processes |

### Settings

| Method | Description |
|--------|-------------|
| `get_settings()` | Get comprehensive device settings |
| `update_settings(config)` | Update settings |

### Health

| Method | Description |
|--------|-------------|
| `health_check()` | Check if API is responsive |

## Running the Example

```bash
# Install dependency
pip install requests

# Run the example
python ztr_client.py
```

## Error Handling

```python
from ztr_client import ZTROClient, ZTROSError

client = ZTROClient()

try:
    state = client.get_hardware_state()
except ZTROSError as e:
    print(f"API Error: {e.message} (status: {e.status_code})")
except ConnectionError:
    print("Cannot connect to ZTR_OS API")
```

## Context Manager

```python
with ZTROClient("http://localhost:5000") as client:
    state = client.get_hardware_state()
    # client.session is properly closed on exit
```

## Further Reading

- [API Documentation](../../docs/api/README.md)
- [Quick Start Guide](../../docs/quickstart.md)