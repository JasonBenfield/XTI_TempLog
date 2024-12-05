using Microsoft.Extensions.DependencyInjection;
using XTI_TempLog.Abstractions;

namespace XTI_TempLog.Fakes;

public static class FakeExtensions
{
    public static void AddFakeTempLogServices(this IServiceCollection services)
    {
        services.AddSingleton<CurrentSession>();
        services.AddSingleton(_ => new TempLogOptions());
        services.AddSingleton<ThrottledLogsBuilder>();
        services.AddSingleton(sp => sp.GetRequiredService<ThrottledLogsBuilder>().Build());
        services.AddSingleton<FakeTempLog>();
        services.AddSingleton<TempLog>(sp => sp.GetRequiredService<FakeTempLog>());
        services.AddScoped<TempLogSession>();
        services.AddSingleton<TempLogRepository>();
        services.AddSingleton<FakeAppEnvironmentContext>();
        services.AddSingleton<IAppEnvironmentContext>(sp => sp.GetRequiredService<FakeAppEnvironmentContext>());
    }
}