# MLP Engine Architecture

This document provides a deep dive into the Multi-Layer Perceptron (MLP) neural network engine that powers ZTR_OS's AI-driven optimization.

## Overview

The MLP engine is a lightweight, fully-connected neural network that analyzes real-time hardware sensor data and produces intelligent control decisions. It is designed to run efficiently on CPU-only environments with online learning capabilities.

## Architecture Diagram

```
┌─────────────────────────────────────────────────────────────────┐
│                    MLP Engine Pipeline                           │
│                                                                   │
│  ┌─────────────────────────────────────────────────────────────┐  │
│  │  1. Sensor Data Collection                                  │  │
│  │     SensorPipeline → HardwareState (every ~1s)              │  │
│  └──────────────────────────┬──────────────────────────────────┘  │
│                              │                                    │
│                              ▼                                    │
│  ┌─────────────────────────────────────────────────────────────┐  │
│  │  2. Feature Extraction                                      │  │
│  │     SensorFeatureExtractor.Extract(state) → 16-dim vector   │  │
│  │     Normalized: [0,1] range per feature                     │  │
│  └──────────────────────────┬──────────────────────────────────┘  │
│                              │                                    │
│                              ▼                                    │
│  ┌─────────────────────────────────────────────────────────────┐  │
│  │  3. Neural Network Inference                                │  │
│  │     MlpNetwork.Predict(features) → 8-dim action vector      │  │
│  │     ┌─────────┐    ┌─────────┐    ┌─────────┐               │  │
│  │     │ Input   │ →  │ Hidden1 │ →  │ Hidden2 │ → Output       │  │
│  │     │  (16)   │    │  (64)   │    │  (32)   │    (8)          │  │
│  │     │ ReLU    │    │ ReLU    │    │ Sigmoid │               │  │
│  │     └─────────┘    └─────────┘    └─────────┘               │  │
│  └──────────────────────────┬──────────────────────────────────┘  │
│                              │                                    │
│                              ▼                                    │
│  ┌─────────────────────────────────────────────────────────────┐  │
│  │  4. Decision Mapping                                        │  │
│  │     PerformanceDecisionEngine.Decide(decision)              │  │
│  │     Maps [0,1] outputs to concrete hardware actions          │  │
│  └──────────────────────────┬──────────────────────────────────┘  │
│                              │                                    │
│                              ▼                                    │
│  ┌─────────────────────────────────────────────────────────────┐  │
│  │  5. Action Execution                                        │  │
│  │     ModeControl / AffinityManagers / AuraLighting           │  │
│  └─────────────────────────────────────────────────────────────┘  │
│                                                                   │
│  ┌─────────────────────────────────────────────────────────────┐  │
│  │  6. Online Learning (periodic)                              │  │
│  │     OnlineLearner.Train(samples) → updated weights          │  │
│  │     Backpropagation with gradient descent                   │  │
│  └─────────────────────────────────────────────────────────────┘  │
│                                                                   │
│  ┌─────────────────────────────────────────────────────────────┐  │
│  │  7. Logging                                                  │  │
│  │     DecisionLogger → MlpDecision[]                          │  │
│  └─────────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────┘
```

## Neural Network Design

### Network Architecture

```
Input Layer:  16 neurons  (sensor features)
Hidden Layer 1: 64 neurons  (ReLU activation)
Hidden Layer 2: 32 neurons  (ReLU activation)
Output Layer:  8 neurons   (Sigmoid activation, normalized [0,1])
```

### Layer Sizes

The second hidden layer size is auto-calculated as `max(HiddenLayerSize / 2, 16)`. With the default `HiddenLayerSize = 64`, this gives 32 neurons.

### Activation Functions

| Layer | Activation | Purpose |
|-------|-----------|---------|
| Hidden 1 | ReLU (`max(0, x)`) | Non-linearity, sparse activation |
| Hidden 2 | ReLU (`max(0, x)`) | Non-linearity, efficient training |
| Output | Sigmoid (`1/(1+e^-x)`) | Normalize outputs to [0,1] range |

### Weight Initialization

Weights are initialized using He initialization (also known as Kaiming initialization):

```
std = sqrt(2.0 / fanIn)
weight = random(-1, 1) * std
```

This is optimized for ReLU networks, ensuring stable gradient flow.

## Input Features (16-Dimensional)

The `SensorFeatureExtractor` normalizes the following features:

| Index | Feature | Source | Normalization |
|-------|---------|--------|---------------|
| 0 | CPU Temperature | `CpuState.Temperature` | /110°C |
| 1 | CPU Usage | `CpuState.Usage` | /100% |
| 2 | CPU Power | `CpuState.Power` | /500W |
| 3 | CPU Clock | `CpuState.ClockMHz` | /10000MHz |
| 4 | CPU Power Limit | `CpuState.PowerLimit` | /500W |
| 5 | GPU Temperature | `GpuState.Temperature` | /110°C |
| 6 | GPU Hotspot Temp | `GpuState.HotspotTemperature` | /120°C |
| 7 | GPU Usage | `GpuState.Usage` | /100% |
| 8 | GPU Power | `GpuState.Power` | /1000W |
| 9 | GPU Core Clock | `GpuState.CoreClockMHz` | /5000MHz |
| 10 | GPU Memory Clock | `GpuState.MemoryClockMHz` | /5000MHz |
| 11 | GPU VRAM Used | `GpuState.UsedVramMB` | /32768MB |
| 12 | Battery Charge | `BatteryState.ChargePercent` | /100% |
| 13 | Charging Status | `BatteryState.IsCharging` | 0 or 1 |
| 14 | CPU Fan Speed | `FanState.CpuFanSpeed` | /100% |
| 15 | GPU Fan Speed | `FanState.GpuFanSpeed` | /100% |

## Output Actions (8-Dimensional)

Each output dimension maps to a concrete hardware action:

| Index | Action | Type | Range | Description |
|-------|--------|------|-------|-------------|
| 0 | SPL Adjustment | `int` | [15, 65] W | CPU short power limit |
| 1 | Fan Curve Offset | `int` | [0, 100] % | Global fan speed adjustment |
| 2 | GPU Clock Offset | `int` | [-300, 300] MHz | GPU core clock adjustment |
| 3 | CPU Clock Offset | `int` | [-500, 500] MHz | CPU clock adjustment |
| 4 | GPU Mode | `GpuMode` | [Eco, Perf, Turbo, Max] | GPU operating mode |
| 5 | CPU Affinity | `int` | [0, 3] | CPU affinity group assignment |
| 6 | GPU Affinity | `int` | [0, 3] | GPU affinity group assignment |
| 7 | Boost Level | `int` | [0, 3] | Overall boost intensity |

### Action Mapping

The `PerformanceDecisionEngine` maps normalized [0,1] outputs:

```csharp
int MapToRange(double normalized, int min, int max)
{
    double clamped = Math.Clamp(normalized, 0.0, 1.0);
    return (int)Math.Round(min + clamped * (max - min));
}
```

### Safety Clamping

All actions are validated against configured safety limits:

```csharp
// SPL clamped to [minSpl, maxSpl] (default: 15-65W)
// Fan speed clamped to [minFanSpeed, maxFanSpeed] (default: 0-100%)
// Clock offsets clamped to safe ranges
// Boost level clamped to [0, 3]
```

## Online Learning

### Training Loop

The `OnlineLearner` performs periodic backpropagation:

1. **Collect samples** — Store (input_features, observed_outcome) pairs during operation
2. **Calculate targets** — Compare actual outcomes with predictions
3. **Forward pass** — Compute current predictions
4. **Backpropagation** — Update weights using gradient descent
5. **Learning rate decay** — Gradually reduce learning rate over time

### Learning Parameters

| Parameter | Default | Description |
|-----------|---------|-------------|
| `LearningRate` | 0.001 | Gradient descent step size |
| `LearningIntervalSeconds` | 30 | Training cycle frequency |
| `PredictionWindowMs` | 500 | Future prediction horizon |

### Weight Update

Weights are updated using standard backpropagation:

```
ΔW = -learningRate × ∂Error/∂W
W_new = W_old + ΔW
```

The loss function is Mean Squared Error (MSE) between predicted and actual action values.

## Decision Logging

### Decision Structure

Each `MlpDecision` records:

| Field | Type | Description |
|-------|------|-------------|
| `Timestamp` | `DateTime` | When the decision was made |
| `InputFeatures` | `double[]` | The 16-dim feature vector used |
| `OutputActions` | `double[]` | The 8-dim action vector produced |
| `ActionType` | `string` | Category of action |
| `Confidence` | `double` | Normalized confidence score (0-1) |
| `Reasoning` | `string` | Human-readable explanation |

### Retrieving Decisions

Use the [MLP API](api/mlp.md) to retrieve decision history:

```bash
# Get last 50 decisions (default)
curl http://localhost:5000/api/mlp/decisions

# Get last 10 decisions
curl "http://localhost:5000/api/mlp/decisions?count=10"
```

## Configuration

### MlpConfig Settings

| Setting | Default | Description |
|---------|---------|-------------|
| `Enabled` | `true` | Master enable/disable |
| `InputSize` | `16` | Must match feature extractor output |
| `HiddenLayerSize` | `64` | First hidden layer neurons |
| `OutputSize` | `8` | Must match action count |
| `LearningRate` | `0.001` | Training step size |
| `LearningIntervalSeconds` | `30` | Training frequency |
| `PredictionWindowMs` | `500` | Prediction horizon |
| `AutoModeSwitch` | `true` | Auto-switch performance modes |
| `AutoAffinity` | `true` | Auto-manage process affinity |

### Adjusting Configuration

Use the [MLP API](api/mlp.md#update-mlp-configuration) to adjust settings at runtime:

```bash
# Increase network capacity and reduce learning rate
curl -X PUT http://localhost:5000/api/mlp/config \
  -H "Content-Type: application/json" \
  -d '{
    "config": {
      "hiddenLayerSize": 128,
      "learningRate": 0.0001,
      "autoModeSwitch": true
    }
  }'
```

## Manual Override

Users can override AI decisions through the `ManualOverride` class:

- When a manual override is active, the MLP engine continues to run and log decisions but does not apply them to hardware
- Manual mode changes (e.g., switching performance modes via the API) temporarily suspend AI control
- The MLP engine resumes control after a configurable timeout or when explicitly re-enabled

## Performance Considerations

| Operation | Typical Latency |
|-----------|----------------|
| Feature extraction | <1ms |
| Network inference (16→64→32→8) | <1ms |
| Decision mapping | <0.5ms |
| Full decision cycle | ~1ms |
| Training cycle (30s interval) | ~50ms |

The MLP engine is designed for real-time operation with minimal CPU overhead. On modern CPUs, the entire inference pipeline takes under 1ms, making it suitable for sub-second decision cycles.

## Extending the MLP

### Adding New Features

1. Update `SensorFeatureExtractor` to include the new data
2. Adjust `InputSize` in `MlpConfig`
3. Retrain the network with the expanded feature set

### Adding New Actions

1. Add a new dimension to the output vector
2. Update `PerformanceDecisionEngine.Decide()` to handle the new action
3. Adjust `OutputSize` in `MlpConfig`
4. Update the safety clamping logic

### Persisting Weights

The network supports weight import/export for persistence across restarts:

```csharp
var (w1, b1, w2, b2, w3, b3) = network.GetWeights();
// Save to file or database...
network.SetWeights(w1, b1, w2, b2, w3, b3);
```