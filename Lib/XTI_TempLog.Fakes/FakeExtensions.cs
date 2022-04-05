using Microsoft.Extensions.DependencyInjection;

namespace XTI_TempLog.Fakes;

public static class FakeExtensions
{
    public static void AddFakeTempLogServices(this IServiceCollection services)
    {
        services.AddSingleton<CurrentSession>();
        services.AddSingleton<TempLog, FakeTempLog>();
        services.AddSingleton(_ => new TempLogOptions());
        services.AddSingleton<ThrottledLogs>();
        services.AddScoped<TempLogSession>();
    }
}