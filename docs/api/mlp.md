# MLP Engine API

Endpoints for managing the Multi-Layer Perceptron neural network configuration and retrieving AI decision history.

The MLP engine analyzes hardware sensor data and makes intelligent decisions about performance tuning, fan curves, GPU modes, and process affinity in real time.

## Get MLP Configuration

Retrieves the current MLP engine configuration.

```
GET /api/mlp/config
```

### Response

```json
{
  "success": true,
  "data": {
    "enabled": true,
    "inputSize": 16,
    "hiddenLayerSize": 64,
    "outputSize": 8,
    "learningRate": 0.001,
    "learningIntervalSeconds": 30,
    "predictionWindowMs": 500,
    "autoModeSwitch": true,
    "autoAffinity": true
  }
}
```

### MlpConfig Fields

| Field | Type | Default | Description |
|-------|------|---------|-------------|
| `enabled` | `bool` | `true` | Whether the MLP engine is active |
| `inputSize` | `int` | `16` | Number of input features (sensor readings) |
| `hiddenLayerSize` | `int` | `64` | Size of first hidden layer |
| `outputSize` | `int` | `8` | Number of output actions |
| `learningRate` | `double` | `0.001` | Learning rate for online training |
| `learningIntervalSeconds` | `int` | `30` | Interval between training cycles |
| `predictionWindowMs` | `int` | `500` | Prediction window in milliseconds |
| `autoModeSwitch` | `bool` | `true` | Auto-switch performance modes |
| `autoAffinity` | `bool` | `true` | Auto-manage CPU/GPU affinity |

---

## Update MLP Configuration

Updates the MLP engine configuration. Changes are applied immediately.

```
PUT /api/mlp/config
```

### Request Body

```json
{
  "config": {
    "enabled": true,
    "inputSize": 16,
    "hiddenLayerSize": 128,
    "outputSize": 8,
    "learningRate": 0.0005,
    "learningIntervalSeconds": 60,
    "predictionWindowMs": 1000,
    "autoModeSwitch": false,
    "autoAffinity": true
  }
}
```

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `config` | `MlpConfig` object | Yes | The new configuration values |

### Response

```json
{
  "success": true,
  "error": null
}
```

### Example

```bash
# Disable MLP auto mode switching
curl -X PUT http://localhost:5000/api/mlp/config \
  -H "Content-Type: application/json" \
  -d '{"config": {"autoModeSwitch": false, "autoAffinity": true}}'
```

---

## Get MLP Decision History

Retrieves recent AI decisions made by the MLP engine.

```
GET /api/mlp/decisions?count=50
```

### Query Parameters

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `count` | `int` | `50` | Number of recent decisions to retrieve |

### Response

```json
{
  "success": true,
  "data": [
    {
      "timestamp": "2025-01-15T10:30:00Z",
      "inputFeatures": [65.0, 42.0, 45.0, 58.0, 78.0, 120.0, 75.0, 45.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0],
      "outputActions": [0.65, 0.45, 0.30, 0.55, 0.50, 0.75, 0.25, 0.67],
      "actionType": "Combined",
      "confidence": 0.87,
      "reasoning": "High GPU usage detected, suggesting performance mode"
    }
  ]
}
```

### MlpDecision Fields

| Field | Type | Description |
|-------|------|-------------|
| `timestamp` | `DateTime` | When the decision was made |
| `inputFeatures` | `double[]` | Normalized sensor values fed to the network |
| `outputActions` | `double[]` | Normalized action values from the network (8 dimensions) |
| `actionType` | `string` | Description of the action category |
| `confidence` | `double` | Decision confidence (0.0 to 1.0) |
| `reasoning` | `string` | Human-readable explanation of the decision |

### Output Action Dimensions

| Index | Action | Range | Description |
|-------|--------|-------|-------------|
| 0 | SPL Adjustment | [0,1] → [15W, 65W] | CPU short power limit |
| 1 | Fan Curve Offset | [0,1] → [0%, 100%] | Fan speed adjustment |
| 2 | GPU Clock Offset | [0,1] → [-300, 300] MHz | GPU clock adjustment |
| 3 | CPU Clock Offset | [0,1] → [-500, 500] MHz | CPU clock adjustment |
| 4 | GPU Mode | [0,1] → [Eco, Performance, Turbo, Max] | GPU operating mode |
| 5 | CPU Affinity | [0,1] → Group 0-3 | CPU affinity group |
| 6 | GPU Affinity | [0,1] → Group 0-3 | GPU affinity group |
| 7 | Boost Level | [0,1] → [0, 3] | Boost intensity |

### Example

```bash
# Get last 10 decisions
curl "http://localhost:5000/api/mlp/decisions?count=10"
```

---

## Get MLP Status

Returns whether the MLP engine is currently enabled.

```
GET /api/mlp/status
```

### Response

```json
{
  "success": true,
  "data": true
}
```

### Example

```bash
curl http://localhost:5000/api/mlp/status
```

---

## How the MLP Works

The MLP engine operates on a continuous loop:

1. **Sensor Collection** — Hardware sensors are polled every ~1 second by the `SensorPipeline`
2. **Feature Extraction** — The `SensorFeatureExtractor` normalizes sensor readings into a 16-dimensional feature vector
3. **Prediction** — The `MlpNetwork` forward-pass produces 8 normalized action values
4. **Decision Engine** — `PerformanceDecisionEngine` maps output values to concrete hardware actions with safety clamps
5. **Online Learning** — Periodically (every `learningIntervalSeconds`), the `OnlineLearner` updates network weights based on observed outcomes
6. **Logging** — All decisions are stored in `DecisionLogger` for history retrieval

See the [MLP Engine Architecture](../mlp-engine.md) for detailed implementation information.