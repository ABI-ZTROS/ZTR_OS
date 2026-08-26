using System.IO;
using System.Text.Json;

namespace ZTR.Desktop.Features.Config.Services;

public class ConfigurationService : IConfigurationService
{
    private readonly Dictionary<string, object> _cache = new();
    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    private static string ConfigPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "io.NET.ZTR_OS", "app-config.json");

    public T? Get<T>(string key) where T : class
    {
        if (_cache.TryGetValue(key, out var value) && value is T typed)
            return typed;
        return null;
    }

    public void Set<T>(string key, T value) where T : class
    {
        _cache[key] = value;
    }

    public void Load()
    {
        try
        {
            if (File.Exists(ConfigPath))
            {
                var json = File.ReadAllText(ConfigPath);
                using var doc = JsonDocument.Parse(json);
                foreach (var prop in doc.RootElement.EnumerateObject())
                {
                    _cache[prop.Name] = JsonSerializer.Deserialize<object>(prop.Value.GetRawText())!;
                }
            }
        }
        catch (Exception ex)
        {
            ForceLog.Write($"[CONFIG] 加载配置失败: {ex.Message}");
        }
    }

    public void Save()
    {
        try
        {
            var dir = Path.GetDirectoryName(ConfigPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var json = JsonSerializer.Serialize(_cache, JsonOpts);
            File.WriteAllText(ConfigPath, json);
        }
        catch (Exception ex)
        {
            ForceLog.Write($"[CONFIG] 保存配置失败: {ex.Message}");
        }
    }
}
