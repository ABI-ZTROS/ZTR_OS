namespace ZTR.Models;

public class UserSettings
{
    public bool AutoPerformance { get; set; } = true;
    public bool AutoMlp { get; set; } = true;
    public bool AutoAura { get; set; } = true;
    public int PollingInterval { get; set; } = 2000;
    public string Theme { get; set; } = "cyber";
    public bool NotificationsEnabled { get; set; } = true;
    public bool AutoStart { get; set; } = false;
    public bool MinimizeToTray { get; set; } = true;
    public int PredictionWindow { get; set; } = 50;
    public bool AutoModeSwitch { get; set; } = true;
    public List<HotkeySetting> Hotkeys { get; set; } = new();
}

public class HotkeySetting
{
    public string Id { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public List<string> Keys { get; set; } = new();
}
