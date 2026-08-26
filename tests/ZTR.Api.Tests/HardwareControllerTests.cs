using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using ZTR.Api;
using ZTR.Api.DTOs;
using ZTR.Models;

namespace ZTR.Api.Tests;

public class HardwareControllerTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private readonly JsonSerializerOptions _jsonOpts = new() { PropertyNameCaseInsensitive = true };

    public HardwareControllerTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetState_ReturnsFrontendCompatibleFormat()
    {
        var response = await _client.GetAsync("/api/hardware/state");

        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ApiResponse<HardwareResponse>>(json, _jsonOpts);

        Assert.NotNull(result);
        Assert.True(result!.Success);
        Assert.NotNull(result.Data);
        Assert.NotNull(result.Data!.Cpu);
        Assert.NotNull(result.Data.Gpu);
        Assert.NotNull(result.Data.Battery);
        Assert.NotNull(result.Data.Fans);
        Assert.NotNull(result.Data.Memory);
    }

    [Fact]
    public async Task GetState_CpuHasCorrectFieldNames()
    {
        var response = await _client.GetAsync("/api/hardware/state");
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ApiResponse<HardwareResponse>>(json, _jsonOpts);

        var cpu = result!.Data!.Cpu;
        Assert.NotNull(cpu);
        Assert.IsType<int>(cpu.Temperature);
        Assert.IsType<int>(cpu.PowerDraw);
        Assert.IsType<int>(cpu.Usage);
        Assert.IsType<int>(cpu.CoreCount);
        Assert.IsType<int>(cpu.ThreadCount);
        Assert.NotNull(cpu.Cores);
    }

    [Fact]
    public async Task GetState_GpuHasCorrectFieldNames()
    {
        var response = await _client.GetAsync("/api/hardware/state");
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ApiResponse<HardwareResponse>>(json, _jsonOpts);

        var gpu = result!.Data!.Gpu;
        Assert.NotNull(gpu);
        Assert.IsType<int>(gpu.Temperature);
        Assert.IsType<int>(gpu.PowerDraw);
        Assert.IsType<int>(gpu.ClockSpeed);
        Assert.IsType<long>(gpu.MemoryUsed);
        Assert.IsType<long>(gpu.MemoryTotal);
    }

    [Fact]
    public async Task GetState_BatteryHasPercentageField()
    {
        var response = await _client.GetAsync("/api/hardware/state");
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ApiResponse<HardwareResponse>>(json, _jsonOpts);

        var battery = result!.Data!.Battery;
        Assert.NotNull(battery);
        Assert.IsType<int>(battery.Percentage);
        Assert.NotNull(battery.Status);
    }

    [Fact]
    public async Task GetState_FansAreArray()
    {
        var response = await _client.GetAsync("/api/hardware/state");
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ApiResponse<HardwareResponse>>(json, _jsonOpts);

        var fans = result!.Data!.Fans;
        Assert.NotNull(fans);
        Assert.True(fans.Count >= 2);
        Assert.Contains(fans, f => f!.Name!.Contains("CPU", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(fans, f => f!.Name!.Contains("GPU", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task GetCpu_ReturnsCpuResponse()
    {
        var response = await _client.GetAsync("/api/hardware/cpu");
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ApiResponse<CpuResponse>>(json, _jsonOpts);

        Assert.NotNull(result);
        Assert.True(result!.Success);
        Assert.NotNull(result.Data);
        Assert.IsType<int>(result.Data!.PowerDraw);
    }

    [Fact]
    public async Task GetGpu_ReturnsGpuResponse()
    {
        var response = await _client.GetAsync("/api/hardware/gpu");
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ApiResponse<GpuResponse>>(json, _jsonOpts);

        Assert.NotNull(result);
        Assert.True(result!.Success);
        Assert.NotNull(result.Data);
    }

    [Fact]
    public async Task GetBattery_ReturnsBatteryResponse()
    {
        var response = await _client.GetAsync("/api/hardware/battery");
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ApiResponse<BatteryResponse>>(json, _jsonOpts);

        Assert.NotNull(result);
        Assert.True(result!.Success);
        Assert.NotNull(result.Data);
    }

    [Fact]
    public async Task GetFan_ReturnsFanList()
    {
        var response = await _client.GetAsync("/api/hardware/fan");
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ApiResponse<List<FanResponse>>>(json, _jsonOpts);

        Assert.NotNull(result);
        Assert.True(result!.Success);
        Assert.NotNull(result.Data);
        Assert.True(result.Data!.Count >= 2);
    }
}
