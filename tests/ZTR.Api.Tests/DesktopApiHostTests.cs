using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc.Testing;
using ZTR.Models;

namespace ZTR.Api.Tests;

/// <summary>
/// Exercises the pipeline that ZTR.Desktop.ApiServerHost configures.
/// These tests catch DI registration, middleware ordering, and hardware
/// readyness regressions before they reach the desktop shell.
/// </summary>
public class DesktopApiHostTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public DesktopApiHostTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task DesktopHost_HealthEndpoint_ReturnsOk()
    {
        var response = await _client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task DesktopHost_HardwareState_ReturnsSuccessFlag()
    {
        var response = await _client.GetAsync("/api/hardware/state");
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ApiResponse<HardwareState>>(json, _jsonOptions);

        Assert.NotNull(result);
        Assert.True(result.Success, $"Hardware state failed: {result!.Error}");
        Assert.NotNull(result.Data);
    }

    [Fact]
    public async Task DesktopHost_AllHardwareEndpoints_Respond()
    {
        var endpoints = new[]
        {
            "/api/hardware/state",
            "/api/hardware/cpu",
            "/api/hardware/gpu",
            "/api/hardware/battery",
            "/api/hardware/fan",
        };

        foreach (var endpoint in endpoints)
        {
            var response = await _client.GetAsync(endpoint);
            Assert.True(response.IsSuccessStatusCode,
                $"Endpoint {endpoint} returned {(int)response.StatusCode}");
        }
    }

    [Fact]
    public async Task DesktopHost_Swagger_IsExposed()
    {
        var response = await _client.GetAsync("/swagger/v1/swagger.json");
        Assert.True(response.IsSuccessStatusCode);
    }

    [Fact]
    public async Task DesktopHost_Swagger_HasMappedControllers()
    {
        var response = await _client.GetAsync("/swagger/v1/swagger.json");
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var paths = doc.RootElement.GetProperty("paths");
        Assert.True(paths.EnumerateObject().Any(),
            "Swagger spec should have mapped controllers but paths is empty. " +
            "This means AddApplicationPart or MapControllers failed to discover controllers.");
    }

    [Fact]
    public async Task DesktopHost_Settings_ReturnsSuccessFlag()
    {
        var response = await _client.GetAsync("/api/settings");
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        Assert.Contains("success", json);
    }

    [Fact]
    public async Task DesktopHost_Diagnostics_EndpointReportsHardwareReadiness()
    {
        var response = await _client.GetAsync("/api/diagnostics");
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        Assert.Contains("success", json);
        Assert.Contains("aspiAvailable", json, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("acpiAvailable", json, StringComparison.OrdinalIgnoreCase);
    }
}
