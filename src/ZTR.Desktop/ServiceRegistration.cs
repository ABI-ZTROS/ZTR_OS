using Microsoft.Extensions.DependencyInjection;

namespace ZTR.Desktop;

public static class ServiceRegistration
{
    public static void Register<TService, TImpl>(IServiceCollection services, BootStats stats)
        where TService : class
        where TImpl : class, TService
    {
        try
        {
            services.AddSingleton<TService, TImpl>();
            stats.Ok++;
        }
        catch
        {
            stats.Fail++;
            throw;
        }
    }

    public static void RegisterInstance<TService>(IServiceCollection services, Func<IServiceProvider, TService> factory, BootStats stats)
        where TService : class
    {
        try
        {
            services.AddSingleton<TService>(factory);
            stats.Ok++;
        }
        catch
        {
            stats.Fail++;
            throw;
        }
    }

    public static void RegisterType<TImpl>(IServiceCollection services, BootStats stats)
        where TImpl : class
    {
        try
        {
            services.AddSingleton<TImpl>();
            stats.Ok++;
        }
        catch
        {
            stats.Fail++;
            throw;
        }
    }
}
