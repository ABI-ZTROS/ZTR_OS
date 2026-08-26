namespace ZTR.Desktop.Features.Config.Services;

public interface IConfigurationService
{
    T? Get<T>(string key) where T : class;
    void Set<T>(string key, T value) where T : class;
    void Load();
    void Save();
}
