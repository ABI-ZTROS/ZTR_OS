using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using ZTR.Api;
using ZTR.Models;

namespace ZTR.Api.Tests;

public class PerformanceControllerTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public PerformanceControllerTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetMode_ReturnsCurrentMode()
    {
        var response = await _client.GetAsync("/api/performance/mode");

        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ApiResponse<AsusMode>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.NotNull(result);
        Assert.True(result.Success);
    }

    [Fact]
    public async Task SetMode_ReturnsApiResponseFormat()
    {
        var request = new SetPerformanceModeRequest("balanced");
        var content = new StringContent(
            JsonSerializer.Serialize(request),
            System.Text.Encoding.UTF8,
            "application/json");

        var response = await _client.PostAsync("/api/performance/mode", content);

        var json = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ApiResponse>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.NotNull(result);
        Assert.False(string.IsNullOrEmpty(json));
        Assert.Contains("success", json);
    }

    [Fact]
    public async Task GetFanCurves_ReturnsCurveData()
    {
        var response = await _client.GetAsync("/api/performance/fan-curves");

        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ApiResponse<object>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
    }

    [Fact]
    public async Task SetPowerLimits_ReturnsApiResponseFormat()
    {
        var request = new SetPowerLimitRequest(35, 25, 15);
        var content = new StringContent(
            JsonSerializer.Serialize(request),
            System.Text.Encoding.UTF8,
            "application/json");

        var response = await _client.PostAsync("/api/performance/power-limits", content);

        var json = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ApiResponse>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.NotNull(result);
        Assert.False(string.IsNullOrEmpty(json));
        Assert.Contains("success", json);
    }
}