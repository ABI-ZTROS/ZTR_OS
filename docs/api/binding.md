# Process Binding & Topology API

Endpoints for monitoring running processes, managing CPU/GPU affinity, and querying hardware topology.

## List Tracked Processes

Retrieves all currently tracked processes and their binding status.

```
GET /api/binding/processes
```

### Response

```json
{
  "success": true,
  "data": [
    {
      "processId": 1234,
      "processName": "Cyberpunk2077.exe",
      "mainWindowTitle": "Cyberpunk 2077",
      "cpuAffinity": {
        "enabled": true,
        "affinityMask": 65535,
        "coreIndices": [0, 1, 2, 3, 4, 5, 6, 7],
        "useNumaNode": false,
        "numaNodeId": 0
      },
      "gpuAffinity": {
        "enabled": false,
        "gpuIndex": 0,
        "engineId": 0
      },
      "strategy": "MlpDriven"
    }
  ]
}
```

### ProcessBinding Fields

| Field | Type | Description |
|-------|------|-------------|
| `processId` | `int` | Windows process ID |
| `processName` | `string` | Process executable name |
| `mainWindowTitle` | `string` | Main window title (empty if no window) |
| `cpuAffinity` | `CpuAffinityConfig` | CPU affinity configuration |
| `gpuAffinity` | `GpuAffinityConfig` | GPU affinity configuration |
| `strategy` | `BindingStrategy` | Current binding strategy |

### BindingStrategy Enum

| Value | Name | Description |
|-------|------|-------------|
| 0 | `Manual` | Manually configured by user |
| 1 | `MlpDriven` | MLP engine manages affinity dynamically |
| 2 | `AutoGame` | Auto-detected as a game, optimized for gaming |
| 3 | `AutoBalanced` | Auto-balanced for general workloads |

---

## Set CPU Affinity

Sets CPU affinity for a specific process, restricting it to use only the specified logical processors.

```
POST /api/binding/cpu
```

### Request Body

```json
{
  "processId": 1234,
  "affinityMask": 255
}
```

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `processId` | `int` | Yes | Target process ID |
| `affinityMask` | `long` | Yes | Bitmask of allowed CPU cores (e.g., 255 = cores 0-7) |

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
  "error": "Failed to set CPU affinity for process 1234"
}
```

### Examples

**Bind process to cores 0-3 only:**
```bash
curl -X POST http://localhost:5000/api/binding/cpu \
  -H "Content-Type: application/json" \
  -d '{"processId": 1234, "affinityMask": 15}'
```

**Bind to cores 8-15 (mask = 65280 = 0xFF00):**
```bash
curl -X POST http://localhost:5000/api/binding/cpu \
  -H "Content-Type: application/json" \
  -d '{"processId": 1234, "affinityMask": 65280}'
```

**Allow all cores (mask = 2^n - 1):**
```bash
curl -X POST http://localhost:5000/api/binding/cpu \
  -H "Content-Type: application/json" \
  -d '{"processId": 1234, "affinityMask": 18446744073709551615}'
```

---

## Set GPU Affinity

Sets GPU affinity for a specific process, restricting it to use a specific GPU or GPU engine.

```
POST /api/binding/gpu
```

### Request Body

```json
{
  "processId": 1234,
  "gpuIndex": 0
}
```

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `processId` | `int` | Yes | Target process ID |
| `gpuIndex` | `int` | Yes | GPU index to bind to (0-based) |

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
  "error": "Failed to set GPU affinity for process 1234"
}
```

### Example

```bash
# Bind process to second GPU
curl -X POST http://localhost:5000/api/binding/gpu \
  -H "Content-Type: application/json" \
  -d '{"processId": 1234, "gpuIndex": 1}'
```

---

## Get System Topology

Retrieves the CPU and GPU hardware topology including NUMA nodes, cache hierarchy, and GPU information.

```
GET /api/binding/topology
```

### Response

```json
{
  "success": true,
  "data": {
    "cpu": {
      "totalCores": 16,
      "totalLogicalProcessors": 32,
      "numaNodeCount": 2,
      "numaNodes": [
        {
          "nodeId": 0,
          "affinityMask": 4294967295,
          "processorIndices": [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15]
        },
        {
          "nodeId": 1,
          "affinityMask": 18446744069414584320,
          "processorIndices": [16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31]
        }
      ],
      "cacheLevels": [
        { "level": 1, "sizeKB": 32, "associativity": 8, "sharedProcessors": 1 },
        { "level": 2, "sizeKB": 512, "associativity": 8, "sharedProcessors": 2 },
        { "level": 3, "sizeKB": 40960, "associativity": 16, "sharedProcessors": 16 }
      ]
    },
    "gpu": {
      "gpuCount": 1,
      "gpus": [
        {
          "index": 0,
          "name": "NVIDIA GeForce RTX 4090",
          "vramMB": 24576,
          "engineCount": 4
        }
      ]
    }
  }
}
```

### CpuTopology Fields

| Field | Type | Description |
|-------|------|-------------|
| `totalCores` | `int` | Physical CPU cores |
| `totalLogicalProcessors` | `int` | Logical processors (cores × threads) |
| `numaNodeCount` | `int` | Number of NUMA nodes |
| `numaNodes` | `CpuNumaNode[]` | NUMA node details |
| `cacheLevels` | `CpuCacheLevel[]` | Cache hierarchy |

### GpuTopology Fields

| Field | Type | Description |
|-------|------|-------------|
| `gpuCount` | `int` | Number of detected GPUs |
| `gpus` | `GpuInfo[]` | GPU details |

---

## Process Endpoints

Additional process management endpoints are available under `/api/process`.

### List All Processes

```
GET /api/process
```

Returns the same response as `GET /api/binding/processes`.

### Get Foreground Process

```
GET /api/process/foreground
```

Returns the currently focused/foreground process. Useful for context-aware optimization.

**Success (200):**
```json
{
  "success": true,
  "data": {
    "processId": 5678,
    "processName": "Chrome.exe",
    "mainWindowTitle": "API Documentation - Google Chrome",
    "cpuAffinity": { ... },
    "gpuAffinity": { ... },
    "strategy": "Manual"
  }
}
```

**No Foreground Process (404):**
```json
{
  "success": false,
  "data": null,
  "error": "No foreground process detected"
}
```

### Get GPU-Intensive Processes

```
GET /api/process/gpu-intensive
```

Returns processes that are currently using significant GPU resources.

### Example

```bash
# Find GPU-intensive processes
curl http://localhost:5000/api/process/gpu-intensive

# Get current foreground process
curl http://localhost:5000/api/process/foreground
```