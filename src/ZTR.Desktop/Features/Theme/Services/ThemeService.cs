using System.IO;
using System.Text.Json;
using System.Windows;

namespace ZTR.Desktop.Features.Theme.Services;

public class ThemeService : IThemeService
{
    public bool IsDarkTheme { get; private set; } = true;

    public void SetTheme(bool dark)
    {
        IsDarkTheme = dark;
        ApplyTheme();
        SaveSettings();
    }

    public void ToggleTheme()
    {
        SetTheme(!IsDarkTheme);
    }

    public void ApplyTheme()
    {
        try
        {
            var app = Application.Current;
            if (app == null) return;

            var resources = app.Resources;
            resources["App.Background"] = IsDarkTheme ? "#0a0e1a" : "#f0f2f5";
            resources["App.Foreground"] = IsDarkTheme ? "#e0e6ed" : "#1a1f2e";
            resources["App.Primary"] = "#00d4ff";

            ForceLog.Write($"[THEME] 主题已应用: {(IsDarkTheme ? "暗色" : "亮色")}");
        }
        catch (Exception ex)
        {
            ForceLog.Write($"[THEME] 应用主题失败: {ex.Message}");
        }
    }

    public void LoadSettings()
    {
        try
        {
            var path = GetConfigPath();
            if (File.Exists(path))
            {
                var json = File.ReadAllText(path);
                using var doc = System.Text.Json.JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("isDarkTheme", out var v))
                    IsDarkTheme = v.GetBoolean();
            }
        }
        catch { }
    }

    public void SaveSettings()
    {
        try
        {
            var path = GetConfigPath();
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var json = System.Text.Json.JsonSerializer.Serialize(new { isDarkTheme = IsDarkTheme }, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(path, json);
        }
        catch { }
    }

    private static string GetConfigPath()
    {
        var appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "io.NET.ZTR_OS");
        return Path.Combine(appData, "theme.json");
    }
}
