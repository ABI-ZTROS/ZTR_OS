# Extending the Platform

This guide covers how to extend ZTR_OS with new features, hardware support, and integrations.

## Architecture Principles

Before extending the platform, understand the core design principles:

1. **Layered Architecture** — Models → HAL → Intelligence → API. Dependencies flow downward.
2. **Interface-Driven** — Hardware access is abstracted behind interfaces for testability.
3. **Event-Driven** — Sensor data flows through the `SensorPipeline` and `SensorQueue` via events.
4. **Swappable Components** — All services are registered in DI via `ServiceCollectionExtensions`.

## Adding New Hardware Support

### Step 1: Create a Device Interface

Add an interface in `ZTR.HAL` following the existing pattern:

```csharp
// ZTR.HAL/IMyNewDevice.cs
public interface IMyNewDevice
{
    Task<int> ReadValueAsync();
    Task<bool> WriteValueAsync(int value);
}
```

### Step 2: Implement the Device

Create the implementation, wrapping whatever protocol (ACPI, HID, WMI, etc.):

```csharp
// ZTR.HAL/MyNewDevice.cs
public class MyNewDevice : IMyNewDevice
{
    private readonly AsusAcpi _acpi;

    public MyNewDevice(AsusAcpi acpi)
    {
        _acpi = acpi;
    }

    public async Task<int> ReadValueAsync()
    {
        // Protocol-specific implementation
        return await _acpi.DeviceGetAsync(AsusDevice.MyNewControl);
    }

    public async Task<bool> WriteValueAsync(int value)
    {
        return await _acpi.DeviceSetAsync(AsusDevice.MyNewControl, value);
    }
}
```

### Step 3: Add Device ID to Enums

If using ACPI, add the control ID to `AsusDevice` enum in `ZTR.Models/AsusDevice.cs`:

```csharp
public enum AsusDevice : uint
{
    // ... existing IDs ...
    MyNewControl = 0x00050099,
}
```

### Step 4: Register in DI

Add the service to `ServiceCollectionExtensions.cs`:

```csharp
services.AddSingleton<IMyNewDevice, MyNewDevice>();
```

### Step 5: Add to Sensor Pipeline (Optional)

If the new device produces sensor data, integrate it into `SensorPipeline`:

```csharp
// In SensorPipeline.CollectDataCore():
var newReadings = CollectNewDeviceReadings(timestamp);
readings.AddRange(newReadings);
```

And update the `HardwareState` model if needed.

## Adding New API Endpoints

### Step 1: Create a Controller

Follow the existing controller pattern:

```csharp
// ZTR.Api/Controllers/MyFeatureController.cs
[ApiController]
[Route("api/[controller]")]
public class MyFeatureController : ControllerBase
{
    private readonly IMyNewDevice _device;
    private readonly ILogger<MyFeatureController> _logger;

    public MyFeatureController(IMyNewDevice device, ILogger<MyFeatureController> logger)
    {
        _device = device;
        _logger = logger;
    }

    [HttpGet("status")]
    [ProducesResponseType<ApiResponse<int>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<int>>> GetStatus()
    {
        var value = await _device.ReadValueAsync();
        return Ok(new ApiResponse<int>(true, value));
    }
}
```

### Step 2: Create Request/Response DTOs

Add DTOs to `ZTR.Models/ApiDtos.cs`:

```csharp
public record MyFeatureRequest(int Value);
public record MyFeatureResponse(int Value, string Status);
```

### Step 3: Register Endpoints

Controllers are auto-discovered via `app.MapControllers()` in `Program.cs`. No manual registration needed.

## Adding New SignalR Hubs

### Step 1: Create a Hub

```csharp
// ZTR.Api/Hubs/MyFeatureHub.cs
public class MyFeatureHub : Hub
{
    public async Task Subscribe(string topic)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, topic);
    }
}
```

### Step 2: Map the Hub

Add to `ApplicationBuilderExtensions.MapZTREndpoints()`:

```csharp
endpoints.MapHub<MyFeatureHub>("/hubs/myfeature");
```

### Step 3: Push Data

From any service, inject `IHubCallerClients` and push:

```csharp
public class MyDataPusher
{
    private readonly IHubCallerClients _clients;

    public MyDataPusher(IHubCallerClients clients)
    {
        _clients = clients;
    }

    public async Task PushUpdate(object data)
    {
        await _clients.All.SendCoreAsync("MyUpdate", new object[] { data });
    }
}
```

## Adding New Performance Modes

### Step 1: Extend the Enum

Add to `AsusMode` in `AsusDevice.cs`:

```csharp
public enum AsusMode
{
    // ... existing ...
    PerformanceCustom = 5,
}
```

### Step 2: Implement Mode Logic

Extend `ModeControl.ApplyModeDefaults()`:

```csharp
private void ApplyModeDefaults(AsusMode mode)
{
    switch (mode)
    {
        case AsusMode.PerformanceCustom:
            _powerManager.ApplyCustomPowerDefaults();
            // Custom fan curves, etc.
            break;
        // ... existing cases ...
    }
}
```

### Step 3: Add to DeviceProbe

Update `DeviceProbe` to advertise the new mode in supported features.

## Adding New MLP Features

### Step 1: Add Input Features

Update `SensorFeatureExtractor` to include new sensor data in the feature vector. Adjust `InputSize` in `MlpConfig` accordingly.

### Step 2: Add Output Actions

Extend `PerformanceDecisionEngine.Decide()` to map new output dimensions to hardware actions.

### Step 3: Update MlpConfig

Update defaults and document the new feature dimensions in the [MLP Engine documentation](mlp-engine.md).

## Adding External Integrations

### Webhooks

Add a middleware or service that sends webhook notifications on significant events:

```csharp
public class WebhookNotifier
{
    private readonly HttpClient _httpClient;
    private readonly string _webhookUrl;

    public async Task NotifyAsync(string eventType, object data)
    {
        var payload = new { type = eventType, data, timestamp = DateTime.UtcNow };
        await _httpClient.PostAsJsonAsync(_webhookUrl, payload);
    }
}
```

### Database Logging

Add Entity Framework Core or similar for persistent storage:

```csharp
services.AddDbContext<ZtrDbContext>(options =>
    options.UseSqlServer(connectionString));

services.AddScoped<IDecisionRepository, EfDecisionRepository>();
```

## Testing New Features

### Unit Tests

Follow the test pattern in `tests/`:

```csharp
// tests/ZTR.HAL.Tests/MyNewDeviceTests.cs
public class MyNewDeviceTests
{
    private readonly Mock<AsusAcpi> _acpiMock;
    private readonly MyNewDevice _device;

    public MyNewDeviceTests()
    {
        _acpiMock = new Mock<AsusAcpi>();
        _device = new MyNewDevice(_acpiMock.Object);
    }

    [Fact]
    public async Task ReadValueAsync_ReturnsAcpiValue()
    {
        _acpiMock.Setup(a => a.DeviceGet(AsusDevice.MyNewControl))
            .Returns(42);

        var result = await _device.ReadValueAsync();

        Assert.Equal(42, result);
    }
}
```

### Integration Tests

Use the `TestWebApplicationFactory` pattern from `ZTR.Api.Tests`:

```csharp
// tests/ZTR.Api.Tests/MyFeatureControllerTests.cs
public class MyFeatureControllerTests : IClassFixture<TestWebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public MyFeatureControllerTests(TestWebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetStatus_ReturnsSuccess()
    {
        var response = await _client.GetAsync("/api/myfeature/status");
        response.EnsureSuccessStatusCode();
    }
}
```

## Build and Run

```bash
# Build
dotnet build

# Run tests
dotnet test

# Run API
dotnet run --project src/ZTR.Api

# Run as Windows Service
# See deployment.md for service installation
```

## Best Practices

1. **Follow existing patterns** — Match the coding style, naming conventions, and architecture
2. **Use interfaces** — Abstract hardware access behind interfaces for testability
3. **Register in DI** — Always use `ServiceCollectionExtensions` for service registration
4. **Handle failures gracefully** — Hardware communication can fail; use try-catch and provide fallbacks
5. **Log everything** — Use `ILogger<T>` consistently
6. **Document your API** — Update the relevant files in `docs/api/`
7. **Write tests** — Cover both happy path and failure scenarios
8. **Respect the layered architecture** — Don't bypass the HAL to access hardware directly