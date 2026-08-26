namespace ZTR.Desktop.Features.Theme.Services;

public interface IThemeService
{
    bool IsDarkTheme { get; }
    void SetTheme(bool dark);
    void ToggleTheme();
    void LoadSettings();
    void SaveSettings();
}
