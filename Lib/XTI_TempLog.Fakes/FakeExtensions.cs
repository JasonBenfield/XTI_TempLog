using Microsoft.Extensions.DependencyInjection;

namespace XTI_TempLog.Fakes;

public static class FakeExtensions
{
    public static void AddFakeTempLogServices(this IServiceCollection services)
    {
        services.AddSingleton<CurrentSession>();
        services.AddSingleton<FakeTempLogV1>();
        services.AddSingleton<TempLogV1>(sp => sp.GetRequiredService<FakeTempLogV1>());
        services.AddSingleton(_ => new TempLogOptions());
        services.AddSingleton<ThrottledLogsBuilder>();
        services.AddSingleton(sp => sp.GetRequiredService<ThrottledLogsBuilder>().Build());
        services.AddScoped<TempLogSessionV1>();
        services.AddSingleton<FakeTempLog>();
        services.AddSingleton<TempLog>(sp => sp.GetRequiredService<FakeTempLog>());
        services.AddScoped<TempLogSession>();
        services.AddSingleton(sp => new TempLogRepository("_fake", sp.GetRequiredService<TempLog>()));
    }
}