#!/usr/bin/env python3
"""
ZTR_OS Python Client Example

A comprehensive Python client for the ZTR_OS REST API.
"""

import json
import time
import sys
from dataclasses import dataclass, field
from typing import Optional

import requests

BASE_URL = "http://localhost:5000"


class ZTROSError(Exception):
    """Custom exception for ZTR_OS API errors."""

    def __init__(self, message: str, status_code: int = None):
        self.message = message
        self.status_code = status_code
        super().__init__(self.message)


@dataclass
class CpuState:
    temperature: int
    usage: int
    power: int
    clock_mhz: int
    power_limit: int


@dataclass
class GpuState:
    temperature: int
    hotspot_temperature: int
    usage: int
    power: int
    used_vram_mb: int
    total_vram_mb: int
    core_clock_mhz: int
    memory_clock_mhz: int


@dataclass
class BatteryState:
    charge_percent: int
    is_charging: bool
    charge_limit: int
    status: str


@dataclass
class FanState:
    cpu_fan_speed: int
    cpu_fan_rpm: int
    gpu_fan_speed: int
    gpu_fan_rpm: int
    mid_fan_speed: int


@dataclass
class HardwareState:
    cpu: CpuState
    gpu: GpuState
    battery: BatteryState
    fan: FanState
    timestamp: str


class ZTROClient:
    """Python client for the ZTR_OS REST API."""

    def __init__(self, base_url: str = BASE_URL):
        self.base_url = base_url.rstrip("/")
        self.session = requests.Session()
        self.session.headers.update({"Content-Type": "application/json"})

    def _request(self, method: str, endpoint: str, data: dict = None,
                 params: dict = None) -> dict:
        url = f"{self.base_url}{endpoint}"
        try:
            response = self.session.request(
                method=method,
                url=url,
                json=data,
                params=params,
                timeout=10,
            )
            response.raise_for_status()
            result = response.json()

            if not result.get("success"):
                raise ZTROSError(
                    result.get("error", "API request failed"),
                    response.status_code,
                )

            return result.get("data")

        except requests.exceptions.ConnectionError:
            raise ZTROSError(
                f"Cannot connect to {self.base_url}. Is ZTR_OS running?"
            )
        except requests.exceptions.HTTPError as e:
            error_msg = str(e)
            try:
                body = e.response.json()
                if body.get("error"):
                    error_msg = body["error"]
            except Exception:
                pass
            raise ZTROSError(error_msg, e.response.status_code)

    # ── Hardware State ──────────────────────────────────────────────

    def get_hardware_state(self) -> HardwareState:
        data = self._request("GET", "/api/hardware/state")
        return HardwareState(
            cpu=CpuState(**data["cpu"]),
            gpu=GpuState(**data["gpu"]),
            battery=BatteryState(**data["battery"]),
            fan=FanState(**data["fan"]),
            timestamp=data["timestamp"],
        )

    def get_cpu_state(self) -> CpuState:
        data = self._request("GET", "/api/hardware/cpu")
        return CpuState(**data)

    def get_gpu_state(self) -> GpuState:
        data = self._request("GET", "/api/hardware/gpu")
        return GpuState(**data)

    def get_battery_state(self) -> BatteryState:
        data = self._request("GET", "/api/hardware/battery")
        return BatteryState(**data)

    def get_fan_state(self) -> FanState:
        data = self._request("GET", "/api/hardware/fan")
        return FanState(**data)

    # ── Performance ─────────────────────────────────────────────────

    def get_performance_mode(self) -> int:
        return self._request("GET", "/api/performance/mode")

    def set_performance_mode(self, mode: int) -> None:
        self._request("POST", "/api/performance/mode", data={"mode": mode})

    def get_fan_curves(self) -> dict:
        return self._request("GET", "/api/performance/fan-curves")

    def set_fan_curve(self, device: int, curve: list[int]) -> None:
        self._request("POST", "/api/performance/fan-curves",
                      data={"device": device, "curve": curve})

    def set_power_limits(self, spl: int, sppt: int, fppt: int) -> None:
        self._request("POST", "/api/performance/power-limits",
                      data={"spl": spl, "sppt": sppt, "fppt": fppt})

    # ── Aura RGB ────────────────────────────────────────────────────

    def list_aura_modes(self) -> list[str]:
        return self._request("GET", "/api/aura/modes")

    def apply_aura(self, mode: int, zone: int, r: int, g: int, b: int) -> None:
        self._request("POST", "/api/aura/apply",
                      data={"mode": mode, "zone": zone, "r": r, "g": g, "b": b})

    # ── MLP Engine ──────────────────────────────────────────────────

    def get_mlp_config(self) -> dict:
        return self._request("GET", "/api/mlp/config")

    def update_mlp_config(self, config: dict) -> None:
        self._request("PUT", "/api/mlp/config", data={"config": config})

    def get_mlp_decisions(self, count: int = 50) -> list:
        return self._request("GET", "/api/mlp/decisions",
                             params={"count": count})

    def get_mlp_status(self) -> bool:
        return self._request("GET", "/api/mlp/status")

    # ── Process Binding ──────────────────────────────────────────────

    def list_processes(self) -> list:
        return self._request("GET", "/api/binding/processes")

    def set_cpu_affinity(self, process_id: int, affinity_mask: int) -> None:
        self._request("POST", "/api/binding/cpu",
                      data={"processId": process_id, "affinityMask": affinity_mask})

    def set_gpu_affinity(self, process_id: int, gpu_index: int) -> None:
        self._request("POST", "/api/binding/gpu",
                      data={"processId": process_id, "gpuIndex": gpu_index})

    def get_topology(self) -> dict:
        return self._request("GET", "/api/binding/topology")

    # ── Process Monitoring ──────────────────────────────────────────

    def get_foreground_process(self) -> dict:
        return self._request("GET", "/api/process/foreground")

    def get_gpu_intensive_processes(self) -> list:
        return self._request("GET", "/api/process/gpu-intensive")

    # ── Settings ────────────────────────────────────────────────────

    def get_settings(self) -> dict:
        return self._request("GET", "/api/settings")

    def update_settings(self, config: dict) -> None:
        self._request("PUT", "/api/settings", data=config)

    # ── Health ──────────────────────────────────────────────────────

    def health_check(self) -> bool:
        try:
            response = self.session.get(f"{self.base_url}/health", timeout=5)
            return response.status_code == 200
        except Exception:
            return False

    def close(self):
        self.session.close()

    def __enter__(self):
        return self

    def __exit__(self, *args):
        self.close()


# ── Example Usage ────────────────────────────────────────────────────

def example_get_hardware_state():
    """Demonstrate reading hardware state."""
    client = ZTROClient()

    try:
        print("Fetching hardware state...")
        state = client.get_hardware_state()

        print(f"CPU: {state.cpu.temperature}°C, {state.cpu.usage}% used, {state.cpu.power}W")
        print(f"GPU: {state.gpu.temperature}°C, {state.gpu.usage}% used, {state.gpu.power}W")
        print(f"Battery: {state.battery.charge_percent}% {'charging' if state.battery.is_charging else 'on battery'}")
        print(f"Fans: CPU={state.fan.cpu_fan_speed}%, GPU={state.fan.gpu_fan_speed}%")

    except ZTROSError as e:
        print(f"Error: {e}")


def example_set_performance_mode():
    """Demonstrate switching performance modes."""
    client = ZTROClient()

    try:
        current_mode = client.get_performance_mode()
        mode_names = {0: "Silent", 1: "Balanced", 2: "Turbo", 3: "Full Speed", 4: "Manual"}
        print(f"Current mode: {mode_names.get(current_mode, current_mode)}")

        print("Switching to Turbo mode...")
        client.set_performance_mode(2)
        print("Done!")

    except ZTROSError as e:
        print(f"Error: {e}")


def example_set_fan_curve():
    """Demonstrate setting a custom fan curve."""
    client = ZTROClient()

    try:
        fan_curves = client.get_fan_curves()
        print(f"Current CPU fan curve: {fan_curves.get('cpu', [])}")

        custom_curve = [40, 0, 50, 25, 60, 50, 70, 75, 80, 100]
        print(f"Setting custom CPU fan curve...")
        client.set_fan_curve(device=0, curve=custom_curve)
        print("Done!")

    except ZTROSError as e:
        print(f"Error: {e}")


def example_mlp_monitoring():
    """Demonstrate MLP engine monitoring."""
    client = ZTROClient()

    try:
        status = client.get_mlp_status()
        print(f"MLP engine: {'Enabled' if status else 'Disabled'}")

        config = client.get_mlp_config()
        print(f"Network: {config.get('inputSize')}→{config.get('hiddenLayerSize')}→{config.get('outputSize')}")
        print(f"Learning rate: {config.get('learningRate')}")

        decisions = client.get_mlp_decisions(count=5)
        for d in decisions:
            print(f"  [{d['timestamp']}] {d.get('reasoning', '')} (confidence: {d.get('confidence', 0):.2f})")

    except ZTROSError as e:
        print(f"Error: {e}")


def example_monitor_loop(interval: float = 2.0, duration: float = 30.0):
    """Monitor hardware state over time."""
    client = ZTROClient()

    print(f"Monitoring hardware for {duration}s (interval: {interval}s)...")
    start_time = time.time()

    try:
        while time.time() - start_time < duration:
            state = client.get_hardware_state()
            print(
                f"[{state.timestamp}] "
                f"CPU: {state.cpu.temperature}°C/{state.cpu.usage}% | "
                f"GPU: {state.gpu.temperature}°C/{state.gpu.usage}% | "
                f"Batt: {state.battery.charge_percent}%"
            )
            time.sleep(interval)

    except KeyboardInterrupt:
        print("\nMonitoring stopped.")
    except ZTROSError as e:
        print(f"Error: {e}")


def example_process_binding():
    """Demonstrate process binding and topology."""
    client = ZTROClient()

    try:
        topology = client.get_topology()
        cpu_info = topology.get("cpu", {})
        gpu_info = topology.get("gpu", {})
        print(f"CPU: {cpu_info.get('totalCores')} cores, {cpu_info.get('totalLogicalProcessors')} threads")
        print(f"NUMA nodes: {cpu_info.get('numaNodeCount')}")
        print(f"GPUs: {gpu_info.get('gpuCount')}")
        for gpu in gpu_info.get("gpus", []):
            print(f"  GPU {gpu['index']}: {gpu['name']} ({gpu['vramMB']}MB VRAM)")

        processes = client.list_processes()
        print(f"\nTracked processes: {len(processes)}")
        for proc in processes[:5]:
            print(f"  PID {proc['processId']}: {proc['processName']} (strategy: {proc['strategy']})")

        foreground = client.get_foreground_process()
        if foreground:
            print(f"\nForeground: {foreground['processName']}")

    except ZTROSError as e:
        print(f"Error: {e}")


def main():
    """Run all examples."""
    print("=" * 60)
    print("ZTR_OS Python Client Examples")
    print("=" * 60)

    examples = [
        ("Hardware State", example_get_hardware_state),
        ("Performance Mode", example_set_performance_mode),
        ("Fan Curve", example_set_fan_curve),
        ("MLP Monitoring", example_mlp_monitoring),
        ("Process Binding", example_process_binding),
    ]

    for name, fn in examples:
        print(f"\n--- {name} ---")
        try:
            fn()
        except ZTROSError as e:
            print(f"  Skipped: {e}")
        except Exception as e:
            print(f"  Error: {e}")

    print("\n" + "=" * 60)
    print("All examples completed.")


if __name__ == "__main__":
    main()