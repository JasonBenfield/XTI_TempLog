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

    public static async Task RunOldVersion(int numberOfRequests)
    {
        var sp = Setup();
        var cts = new CancellationTokenSource();
        using var sessionScope = sp.CreateScope();
        var tempLogSession = sessionScope.ServiceProvider.GetRequiredService<TempLogSessionV1>();
        await tempLogSession.StartSession();
        var requestTasks = new List<Task>();
        foreach (var i in Enumerable.Range(0, numberOfRequests))
        {
            requestTasks.Add(Task.Run(() => LogRequestOldVersion(sp)));
        }
        await Task.WhenAll(requestTasks);
        await tempLogSession.EndSession();
    }

    private static async Task LogRequestOldVersion(IServiceProvider sp)
    {
        using var scope = sp.CreateScope();
        var tempLogSession = scope.ServiceProvider.GetRequiredService<TempLogSessionV1>();
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
        hostBuilder.Services.AddScoped<TempLogV1>(sp =>
        {
            var dataProtector = sp.GetDataProtector("XTI_TempLog");
            var appDataFolder = sp.GetRequiredService<AppDataFolder>();
            return new DiskTempLogV1(dataProtector, appDataFolder.WithSubFolder("TempLogs").Path());
        });
        hostBuilder.Services.AddScoped<TempLogSessionV1>();
        var host = hostBuilder.Build();
        return host.Scope();
    }

}
