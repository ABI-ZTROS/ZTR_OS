using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace ZTR.HAL;

public class BiosUpdateChecker : IDisposable
{
    private readonly ILogger<BiosUpdateChecker>? _logger;
    private readonly HttpClient _httpClient;
    private bool _disposed;

    public BiosUpdateChecker(ILogger<BiosUpdateChecker>? logger = null)
    {
        _logger = logger;
        _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
    }

    public class UpdateInfo
    {
        public string Component { get; set; } = string.Empty;
        public string CurrentVersion { get; set; } = string.Empty;
        public string LatestVersion { get; set; } = string.Empty;
        public bool HasUpdate { get; set; }
        public string DownloadUrl { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    public async Task<List<UpdateInfo>> CheckForUpdatesAsync(string modelName, CancellationToken ct = default)
    {
        var updates = new List<UpdateInfo>();
        try
        {
            _logger?.LogInformation("Checking for BIOS/driver updates for {Model}", modelName);
            var biosTask = CheckBiosUpdateAsync(modelName, ct);
            var driverTask = CheckDriverUpdatesAsync(modelName, ct);
            await Task.WhenAll(biosTask, driverTask);
            updates.AddRange(await biosTask);
            updates.AddRange(await driverTask);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to check for updates");
        }
        return updates;
    }

    private async Task<List<UpdateInfo>> CheckBiosUpdateAsync(string model, CancellationToken ct)
    {
        var result = new List<UpdateInfo>();
        try
        {
            var url = $"https://www.asus.com/supportapi/api/v1/bios?model={Uri.EscapeDataString(model)}";
            var response = await _httpClient.GetAsync(url, ct);
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync(ct);
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                if (root.TryGetProperty("bios", out var bios))
                {
                    foreach (var item in bios.EnumerateArray())
                    {
                        result.Add(new UpdateInfo
                        {
                            Component = "BIOS",
                            CurrentVersion = item.TryGetProperty("current")?.GetString() ?? "unknown",
                            LatestVersion = item.TryGetProperty("latest")?.GetString() ?? "unknown",
                            HasUpdate = true,
                            DownloadUrl = item.TryGetProperty("url")?.GetString() ?? string.Empty,
                            Description = item.TryGetProperty("description")?.GetString() ?? string.Empty
                        });
                    }
                }
            }
            else
            {
                _logger?.LogWarning("BIOS update check failed with status {Status}", response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogDebug(ex, "BIOS update check error (non-critical)");
        }
        return result;
    }

    private async Task<List<UpdateInfo>> CheckDriverUpdatesAsync(string model, CancellationToken ct)
    {
        var result = new List<UpdateInfo>();
        try
        {
            var url = $"https://www.asus.com/supportapi/api/v1/drivers?model={Uri.EscapeDataString(model)}";
            var response = await _httpClient.GetAsync(url, ct);
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync(ct);
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                if (root.TryGetProperty("drivers", out var drivers))
                {
                    foreach (var item in drivers.EnumerateArray())
                    {
                        result.Add(new UpdateInfo
                        {
                            Component = item.TryGetProperty("name")?.GetString() ?? "Driver",
                            CurrentVersion = item.TryGetProperty("current_version")?.GetString() ?? "unknown",
                            LatestVersion = item.TryGetProperty("version")?.GetString() ?? "unknown",
                            HasUpdate = true,
                            DownloadUrl = item.TryGetProperty("download_url")?.GetString() ?? string.Empty,
                            Description = item.TryGetProperty("description")?.GetString() ?? string.Empty
                        });
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger?.LogDebug(ex, "Driver update check error (non-critical)");
        }
        return result;
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _httpClient.Dispose();
            _disposed = true;
        }
    }
}
