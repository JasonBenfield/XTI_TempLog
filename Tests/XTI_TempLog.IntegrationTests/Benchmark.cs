using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;
using XTI_Core;
using XTI_Core.Extensions;
using XTI_Secrets.Extensions;
using XTI_TempLog.Abstractions;
using XTI_TempLog.Extensions;

namespace XTI_TempLog.IntegrationTests;

public static class Benchmark
{
    public static async Task RunNewVersion(int numberOfRequests)
    {
        var sp = Setup();
        var cts = new CancellationTokenSource();
        var tempLogRepo = sp.GetRequiredService<TempLogRepository>();
        var autoWriteTask = Task.Run(() => tempLogRepo.AutoWriteToLocalStorage(TimeSpan.FromMinutes(1), cts.Token));
        using var sessionScope = sp.CreateScope();
        var tempLogSession = sessionScope.ServiceProvider.GetRequiredService<TempLogSession>();
        await tempLogSession.StartSession();
        var requestTasks = new List<Task>();
        foreach (var i in Enumerable.Range(0, numberOfRequests))
        {
            requestTasks.Add(Task.Run(() => LogRequestNewVersion(sp)));
        }
        await Task.WhenAll(requestTasks);
        await tempLogSession.EndSession();
        cts.Cancel();
        await autoWriteTask.WaitAsync(TimeSpan.FromSeconds(10));
    }

    private static async Task LogRequestNewVersion(IServiceProvider sp)
    {
        using var scope = sp.CreateScope();
        var tempLogSession = scope.ServiceProvider.GetRequiredService<TempLogSession>();
        await tempLogSession.StartRequest("/Test/Current");
        await tempLogSession.EndRequest();
    }

    private static IServiceProvider Setup()
    {
        var hostBuilder = new XtiHostBuilder();
        hostBuilder.Services.AddMemoryCache();
        hostBuilder.Services.AddScoped<IClock, UtcClock>();
        hostBuilder.Services.AddScoped<IAppEnvironmentContext, FakeAppEnvironmentContext>();
        hostBuilder.Services.AddSingleton<CurrentSession>();
        hostBuilder.Services.AddSingleton<XtiFolder>();
        hostBuilder.Services.AddSingleton
        (
            sp =>
                sp.GetRequiredService<XtiFolder>()
                    .AppDataFolder()
                    .WithSubFolder("OldVersion")
        );
        hostBuilder.Services.AddXtiDataProtection();
        hostBuilder.Services.AddTempLogServices();
        var host = hostBuilder.Build();
        return host.Scope();
    }

}
