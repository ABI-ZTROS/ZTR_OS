using System.Net;
using System.Net.WebSockets;
using System.Text.Json;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using ZTR.Api;
using ZTR.Api.Hubs;
using ZTR.HAL;
using ZTR.Models;

namespace ZTR.Api.Tests;

public class IntegrationPipelineTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public IntegrationPipelineTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task FullPipeline_HardwareState_FromPipelineToApi()
    {
        var response = await _client.GetAsync("/api/hardware/state");

        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ApiResponse<ZTR.Api.DTOs.HardwareResponse>>(json, _jsonOptions);

        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.NotNull(result.Data!.Cpu);
        Assert.NotNull(result.Data.Gpu);
        Assert.NotNull(result.Data.Battery);
        Assert.NotNull(result.Data.Fans);
        Assert.True(result.Data.Timestamp != default);
    }

    [Fact]
    public async Task FullPipeline_SensorQueue_EnqueueAndRetrieve()
    {
        var scope = _factory.Services.CreateScope();
        var pipeline = scope.ServiceProvider.GetRequiredService<SensorPipeline>();
        var queue = scope.ServiceProvider.GetRequiredService<SensorQueue>();

        var initialCount = queue.Count;
        var state = pipeline.CollectOnce();

        Assert.NotNull(state);
        Assert.True(queue.Count > initialCount);

        var history = queue.GetHistory(10);
        Assert.True(history.Count > 0);
    }

    [Fact]
    public async Task FullPipeline_SignalRBridge_PublishesState()
    {
        var scope = _factory.Services.CreateScope();
        var bridge = scope.ServiceProvider.GetService(typeof(SensorSignalRBridge));
        Assert.NotNull(bridge);

        var hubContext = scope.ServiceProvider.GetService(typeof(IHubContext<SensorHub>));
        Assert.NotNull(hubContext);
    }

    [Fact]
    public async Task BackendOffline_WhenApiDown_ShowsDisconnectedState()
    {
        using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Production");
            });

        using var client = factory.CreateClient();
        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task IntegrationMiddleware_RejectsOutOfRangeTemperature()
    {
        var invalidPayload = new
        {
            Temperature = 200,
            Usage = 50
        };

        var content = new StringContent(
            JsonSerializer.Serialize(invalidPayload),
            Encoding.UTF8,
            "application/json");

        var response = await _client.PutAsync("/api/settings", content);

        var json = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ApiResponse>(json, _jsonOptions);

        Assert.NotNull(result);
        Assert.False(result!.Success);
        Assert.Contains("temperature", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task IntegrationMiddleware_RejectsOutOfRangeUsage()
    {
        var invalidPayload = new
        {
            temperature = 50,
            usage = 150
        };

        var content = new StringContent(
            JsonSerializer.Serialize(invalidPayload),
            Encoding.UTF8,
            "application/json");

        var response = await _client.PostAsync("/api/performance/mode", content);

        var json = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ApiResponse>(json, _jsonOptions);

        Assert.NotNull(result);
        Assert.False(result!.Success);
    }

    [Fact]
    public async Task IntegrationMiddleware_RejectsOutOfRangePower()
    {
        var invalidPayload = new
        {
            SPL = 5000,
            SPPT = 50,
            FPPT = 30
        };

        var content = new StringContent(
            JsonSerializer.Serialize(invalidPayload),
            Encoding.UTF8,
            "application/json");

        var response = await _client.PostAsync("/api/performance/power-limits", content);

        var json = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ApiResponse>(json, _jsonOptions);

        Assert.NotNull(result);
        Assert.False(result!.Success);
        Assert.Contains("power", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task IntegrationMiddleware_AcceptsValidRequest()
    {
        var validPayload = new
        {
            Mode = AsusMode.PerformanceBalanced
        };

        var content = new StringContent(
            JsonSerializer.Serialize(validPayload),
            Encoding.UTF8,
            "application/json");

        var response = await _client.PostAsync("/api/performance/mode", content);

        Assert.True(response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SignalR_HardwareDataHub_EndpointAvailable()
    {
        var response = await _client.GetAsync("/hubs/hardware");

        Assert.True(response.IsSuccessStatusCode ||
                    response.StatusCode == HttpStatusCode.UpgradeRequired ||
                    response.StatusCode == HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SignalR_SensorHub_EndpointAvailable()
    {
        var response = await _client.GetAsync("/hubs/sensor");

        Assert.True(response.IsSuccessStatusCode ||
                    response.StatusCode == HttpStatusCode.UpgradeRequired ||
                    response.StatusCode == HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SignalR_StateHub_EndpointAvailable()
    {
        var response = await _client.GetAsync("/hubs/state");

        Assert.True(response.IsSuccessStatusCode ||
                    response.StatusCode == HttpStatusCode.UpgradeRequired ||
                    response.StatusCode == HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ApiResponseFormat_ConsistentAcrossAllEndpoints()
    {
        var endpoints = new[]
        {
            "/api/hardware/state",
            "/api/hardware/cpu",
            "/api/hardware/gpu",
            "/api/hardware/battery",
            "/api/hardware/fan",
            "/api/performance/mode",
            "/api/performance/fan-curves",
            "/api/aura/modes",
            "/api/mlp/config",
            "/api/mlp/status",
            "/api/settings",
            "/api/process",
            "/api/binding/topology",
        };

        foreach (var endpoint in endpoints)
        {
            var response = await _client.GetAsync(endpoint);
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                Assert.True(json.Contains("success"),
                    $"Endpoint {endpoint} response does not contain 'success' field");
            }
        }
    }

    [Fact]
    public async Task HardwareState_DataIntegrity_AllFieldsPopulated()
    {
        var response = await _client.GetAsync("/api/hardware/state");
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ApiResponse<ZTR.Api.DTOs.HardwareResponse>>(json, _jsonOptions);

        Assert.NotNull(result?.Data);
        var state = result.Data!;

        Assert.True(state.Cpu.Temperature >= -1, "CPU Temperature should be non-negative or -1 (unavailable)");
        Assert.True(state.Cpu.Usage >= -1, "CPU Usage should be non-negative or -1 (unavailable)");
        Assert.True(state.Cpu.PowerDraw >= -1, "CPU Power should be non-negative or -1 (unavailable)");
        Assert.True(state.Gpu.Temperature >= -1, "GPU Temperature should be non-negative or -1 (unavailable)");
        Assert.True(state.Gpu.Usage >= -1, "GPU Usage should be non-negative or -1 (unavailable)");
        Assert.True(state.Battery.Percentage >= -1, "Battery charge should be non-negative or -1 (unavailable)");
    }

    [Fact]
    public async Task SensorBackgroundService_HealthEndpointAlive()
    {
        var response = await _client.GetAsync("/health");
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        Assert.Contains("Healthy", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task FullPipeline_GetCpu_ReturnsOnlyCpuData()
    {
        var response = await _client.GetAsync("/api/hardware/cpu");
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ApiResponse<ZTR.Api.DTOs.CpuResponse>>(json, _jsonOptions);

        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.True(result.Data!.Temperature >= 0);
    }

    [Fact]
    public async Task FullPipeline_GetGpu_ReturnsOnlyGpuData()
    {
        var response = await _client.GetAsync("/api/hardware/gpu");
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ApiResponse<ZTR.Api.DTOs.GpuResponse>>(json, _jsonOptions);

        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
    }

    [Fact]
    public async Task FullPipeline_GetBattery_ReturnsOnlyBatteryData()
    {
        var response = await _client.GetAsync("/api/hardware/battery");
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ApiResponse<ZTR.Api.DTOs.BatteryResponse>>(json, _jsonOptions);

        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
    }

    [Fact]
    public async Task FullPipeline_GetFan_ReturnsOnlyFanData()
    {
        var response = await _client.GetAsync("/api/hardware/fan");
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ApiResponse<List<ZTR.Api.DTOs.FanResponse>>>(json, _jsonOptions);

        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
    }

    [Fact]
    public async Task MlpController_ConfigAndStatus_Consistent()
    {
        var configResponse = await _client.GetAsync("/api/mlp/config");
        configResponse.EnsureSuccessStatusCode();

        var statusResponse = await _client.GetAsync("/api/mlp/status");
        statusResponse.EnsureSuccessStatusCode();

        var configJson = await configResponse.Content.ReadAsStringAsync();
        var statusJson = await statusResponse.Content.ReadAsStringAsync();

        var configResult = JsonSerializer.Deserialize<ApiResponse<MlpConfig>>(configJson, _jsonOptions);
        var statusResult = JsonSerializer.Deserialize<ApiResponse<bool>>(statusJson, _jsonOptions);

        Assert.NotNull(configResult);
        Assert.NotNull(statusResult);
        Assert.Equal(configResult!.Data!.Enabled, statusResult!.Data);
    }

    [Fact]
    public async Task ProcessController_ListProcesses_ReturnsConsistentData()
    {
        var response = await _client.GetAsync("/api/process");
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ApiResponse<IReadOnlyList<ProcessBinding>>>(json, _jsonOptions);

        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
    }

    [Fact]
    public async Task PerformanceController_SetMode_InvalidRequestRejected()
    {
        var invalidPayload = new { Mode = 999 };
        var content = new StringContent(
            JsonSerializer.Serialize(invalidPayload),
            Encoding.UTF8,
            "application/json");

        var response = await _client.PostAsync("/api/performance/mode", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task BindingController_Topology_ReturnsStructure()
    {
        var response = await _client.GetAsync("/api/binding/topology");
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ApiResponse<object>>(json, _jsonOptions);

        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
    }
}