using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using ZTR.Api;
using ZTR.Models;

namespace ZTR.Api.Tests;

public class HardwareControllerTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public HardwareControllerTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetState_ReturnsApiResponseFormat()
    {
        var response = await _client.GetAsync("/api/hardware/state");

        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ApiResponse<HardwareState>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.NotNull(result.Data!.Cpu);
        Assert.NotNull(result.Data!.Gpu);
        Assert.NotNull(result.Data!.Battery);
        Assert.NotNull(result.Data!.Fan);
    }

    [Fact]
    public async Task GetCpu_ReturnsCpuState()
    {
        var response = await _client.GetAsync("/api/hardware/cpu");

        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ApiResponse<CpuState>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
    }

    [Fact]
    public async Task GetGpu_ReturnsGpuState()
    {
        var response = await _client.GetAsync("/api/hardware/gpu");

        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ApiResponse<GpuState>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
    }

    [Fact]
    public async Task GetBattery_ReturnsBatteryState()
    {
        var response = await _client.GetAsync("/api/hardware/battery");

        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ApiResponse<BatteryState>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
    }

    [Fact]
    public async Task GetFan_ReturnsFanState()
    {
        var response = await _client.GetAsync("/api/hardware/fan");

        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ApiResponse<FanState>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
    }
}