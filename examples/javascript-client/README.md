# ZTR_OS JavaScript Client

A JavaScript/TypeScript client for the ZTR_OS API featuring REST calls and SignalR real-time updates.

## Prerequisites

- Node.js 18+
- npm or yarn

## Installation

```bash
npm install @microsoft/signalr
```

## REST API Client

### Basic Usage

```javascript
const BASE_URL = 'http://localhost:5000';

async function apiCall(endpoint, options = {}) {
  const response = await fetch(`${BASE_URL}${endpoint}`, {
    headers: { 'Content-Type': 'application/json' },
    ...options,
  });

  if (!response.ok) {
    throw new Error(`HTTP ${response.status}: ${response.statusText}`);
  }

  const result = await response.json();

  if (!result.success) {
    throw new Error(result.error || 'API request failed');
  }

  return result.data;
}
```

### Get Hardware State

```javascript
async function getHardwareState() {
  return apiCall('/api/hardware/state');
}

async function getCpuState() {
  return apiCall('/api/hardware/cpu');
}

async function getGpuState() {
  return apiCall('/api/hardware/gpu');
}
```

### Control Performance

```javascript
async function setPerformanceMode(mode) {
  return apiCall('/api/performance/mode', {
    method: 'POST',
    body: JSON.stringify({ mode }),
  });
}

async function setFanCurve(device, curveBytes) {
  return apiCall('/api/performance/fan-curves', {
    method: 'POST',
    body: JSON.stringify({ device, curve: curveBytes }),
  });
}

async function setPowerLimits(spl, sppt, fppt) {
  return apiCall('/api/performance/power-limits', {
    method: 'POST',
    body: JSON.stringify({ spl, sppt, fppt }),
  });
}
```

### Control Aura RGB

```javascript
async function setAuraMode(mode, zone, r, g, b) {
  return apiCall('/api/aura/apply', {
    method: 'POST',
    body: JSON.stringify({ mode, zone, r, g, b }),
  });
}

async function listAuraModes() {
  return apiCall('/api/aura/modes');
}
```

### Manage MLP Engine

```javascript
async function getMlpConfig() {
  return apiCall('/api/mlp/config');
}

async function updateMlpConfig(config) {
  return apiCall('/api/mlp/config', {
    method: 'PUT',
    body: JSON.stringify({ config }),
  });
}

async function getMlpDecisions(count = 50) {
  return apiCall(`/api/mlp/decisions?count=${count}`);
}
```

### Process Binding

```javascript
async function listProcesses() {
  return apiCall('/api/binding/processes');
}

async function setCpuAffinity(processId, affinityMask) {
  return apiCall('/api/binding/cpu', {
    method: 'POST',
    body: JSON.stringify({ processId, affinityMask }),
  });
}

async function setGpuAffinity(processId, gpuIndex) {
  return apiCall('/api/binding/gpu', {
    method: 'POST',
    body: JSON.stringify({ processId, gpuIndex }),
  });
}

async function getTopology() {
  return apiCall('/api/binding/topology');
}
```

### Settings

```javascript
async function getSettings() {
  return apiCall('/api/settings');
}

async function updateSettings(config) {
  return apiCall('/api/settings', {
    method: 'PUT',
    body: JSON.stringify(config),
  });
}
```

---

## SignalR Real-Time Client

### Connect to Sensor Updates

```javascript
import * as signalR from '@microsoft/signalr';

function createSensorHub() {
  const connection = new signalR.HubConnectionBuilder()
    .withUrl('http://localhost:5000/hubs/sensor')
    .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
    .build();

  connection.on('SensorUpdate', (state) => {
    console.log('Hardware update:', state);
    // state = { cpu, gpu, battery, fans, memory, timestamp }
  });

  connection.onclose((error) => {
    console.log('Disconnected:', error?.message);
  });

  return connection;
}

async function startSensorHub() {
  const hub = createSensorHub();

  try {
    await hub.start();
    console.log('Connected to sensor hub');
  } catch (err) {
    console.error('Failed to connect:', err);
  }

  return hub;
}
```

### Connect to State Changes

```javascript
function createStateHub() {
  const connection = new signalR.HubConnectionBuilder()
    .withUrl('http://localhost:5000/hubs/state')
    .withAutomaticReconnect()
    .build();

  connection.on('StateChange', (type, data) => {
    console.log(`State changed [${type}]:`, data);
  });

  return connection;
}
```

### Connect to Hardware Data Hub

```javascript
function createHardwareHub() {
  const connection = new signalR.HubConnectionBuilder()
    .withUrl('http://localhost:5000/hubs/hardware')
    .withAutomaticReconnect()
    .build();

  connection.on('HardwareUpdate', (data) => {
    console.log('Hardware data:', data);
  });

  connection.on('JoinGroup', async (groupName) => {
    await connection.invoke('JoinGroup', groupName);
    console.log('Joined group:', groupName);
  });

  return connection;
}
```

### Complete Example

```javascript
import * as signalR from '@microsoft/signalr';

const BASE_URL = 'http://localhost:5000';

async function main() {
  console.log('ZTR_OS JavaScript Client Example');
  console.log('===================================');

  // REST API calls
  try {
    const state = await getHardwareState();
    console.log('CPU Temp:', state.cpu.temperature, '°C');
    console.log('GPU Usage:', state.gpu.usage, '%');
    console.log('Battery:', state.battery.chargePercent, '%');
  } catch (err) {
    console.error('REST Error:', err.message);
  }

  // SignalR connection
  try {
    const sensorHub = await startSensorHub();

    // Keep running for 30 seconds to receive updates
    console.log('Listening for updates for 30 seconds...');
    await new Promise((resolve) => setTimeout(resolve, 30000));

    await sensorHub.stop();
    console.log('Disconnected.');
  } catch (err) {
    console.error('SignalR Error:', err.message);
  }
}

// Run
main().catch(console.error);
```

## TypeScript Interface Definitions

```typescript
interface CpuState {
  temperature: number;
  usage: number;
  power: number;
  clockMHz: number;
  powerLimit: number;
}

interface GpuState {
  temperature: number;
  hotspotTemperature: number;
  usage: number;
  power: number;
  usedVramMB: number;
  totalVramMB: number;
  coreClockMHz: number;
  memoryClockMHz: number;
}

interface BatteryState {
  chargePercent: number;
  isCharging: boolean;
  chargeLimit: number;
  status: string;
}

interface FanState {
  cpuFanSpeed: number;
  cpuFanRpm: number;
  gpuFanSpeed: number;
  gpuFanRpm: number;
  midFanSpeed: number;
}

interface HardwareState {
  cpu: CpuState;
  gpu: GpuState;
  battery: BatteryState;
  fan: FanState;
  timestamp: string;
}

interface ApiResponse<T> {
  success: boolean;
  data: T | null;
  error: string | null;
}

type AsusMode = 0 | 1 | 2 | 3 | 4;
type AsusFan = 0 | 1 | 2 | 3;
type AuraMode = number;
type AuraZone = 0 | 1 | 2 | 3 | 4 | 5;
```

## Running the Example

```bash
# Install dependencies
npm install @microsoft/signalr

# Run the example
node example.js
```

## Further Reading

- [API Documentation](../../docs/api/README.md)
- [SignalR Documentation](https://learn.microsoft.com/en-us/aspnet/core/signalr/javascript-client)