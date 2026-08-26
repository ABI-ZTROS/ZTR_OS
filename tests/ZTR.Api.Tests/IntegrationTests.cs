using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using ZTR.Api;
using ZTR.Models;

namespace ZTR.Api.Tests;

public class IntegrationTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public IntegrationTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task HealthEndpoint_ReturnsHealthy()
    {
        var response = await _client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task SwaggerEndpoint_IsAvailable()
    {
        var response = await _client.GetAsync("/swagger/v1/swagger.json");

        Assert.True(response.IsSuccessStatusCode);
        var json = await response.Content.ReadAsStringAsync();
        Assert.Contains("ZTR_OS API", json);
    }

    [Fact]
    public async Task GetSettings_ReturnsValidFormat()
    {
        var response = await _client.GetAsync("/api/settings");

        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ApiResponse<object>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.NotNull(result);
        Assert.True(result.Success);
    }

    [Fact]
    public async Task ListProcesses_ReturnsList()
    {
        var response = await _client.GetAsync("/api/process");

        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ApiResponse<IReadOnlyList<ProcessBinding>>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
    }

    [Fact]
    public async Task GetForegroundProcess_Or404()
    {
        var response = await _client.GetAsync("/api/process/foreground");

        Assert.True(response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ListAuraModes_ReturnsModes()
    {
        var response = await _client.GetAsync("/api/aura/modes");

        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ApiResponse<IEnumerable<string>>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Contains("Static", result.Data!);
    }

    [Fact]
    public async Task GetMlpConfig_ReturnsConfig()
    {
        var response = await _client.GetAsync("/api/mlp/config");

        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ApiResponse<MlpConfig>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
    }

    [Fact]
    public async Task GetMlpStatus_ReturnsBoolean()
    {
        var response = await _client.GetAsync("/api/mlp/status");

        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ApiResponse<bool>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.NotNull(result);
        Assert.True(result.Success);
    }

    [Fact]
    public async Task GetBindingTopology_ReturnsTopology()
    {
        var response = await _client.GetAsync("/api/binding/topology");

        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ApiResponse<object>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.NotNull(result);
        Assert.True(result.Success);
    }

    [Fact]
    public async Task ApiResponse_Format_Is_Consistent()
    {
        var endpoints = new[]
        {
            "/api/hardware/state",
            "/api/performance/mode",
            "/api/aura/modes",
            "/api/mlp/config",
            "/api/settings"
        };

        foreach (var endpoint in endpoints)
        {
            var response = await _client.GetAsync(endpoint);
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                Assert.Contains("success", json);
            }
        }
    }
}