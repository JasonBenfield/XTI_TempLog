﻿using Microsoft.Extensions.DependencyInjection;

namespace XTI_TempLog.Fakes;

public static class FakeExtensions
{
    public static void AddFakeTempLogServices(this IServiceCollection services)
    {
        services.AddSingleton<CurrentSession>();
        services.AddSingleton<TempLog, FakeTempLog>();
        services.AddSingleton(_ => new TempLogOptions());
        services.AddSingleton<ThrottledLogsBuilder>();
        services.AddSingleton(sp => sp.GetRequiredService<ThrottledLogsBuilder>().Build());
        services.AddScoped<TempLogSession>();
    }
}