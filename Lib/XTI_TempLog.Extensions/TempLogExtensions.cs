using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;
using XTI_Core;
using XTI_Core.Extensions;

namespace XTI_TempLog.Extensions;

public static class TempLogExtensions
{
    public static void AddTempLogServices(this IServiceCollection services)
    {
        services.AddConfigurationOptions<TempLogOptions>(TempLogOptions.TempLog);
        services.AddThrottledLog((sp, b) => { });
        services.AddSingleton<TempLog>(sp =>
        {
            var dataProtector = sp.GetDataProtector("XTI_TempLog");
            var xtiFolder = sp.GetRequiredService<XtiFolder>();
            return new DiskTempLog(dataProtector, xtiFolder.AppDataFolder().WithSubFolder("TempLogs").Path());
        });
        services.AddSingleton<TempLogRepository>();
        services.AddScoped<TempLogSession>();
    }

    public static void AddTempLogWriterHostedService(this IServiceCollection services)
    {
        services.AddHostedService<TempLogWriterHostedService>();
    }

    public static void AddThrottledLog(this IServiceCollection services, Action<IServiceProvider, ThrottledLogsBuilder> action)
    {
        var serviceDescriptors = services
            .Where(s => s.ServiceType == typeof(ThrottledLogs))
            .ToArray();
        foreach (var serviceDescriptor in serviceDescriptors)
        {
            services.Remove(serviceDescriptor);
        }
        services.AddSingleton
        (
            sp =>
            {
                var clock = sp.GetRequiredService<IClock>();
                var builder = new ThrottledLogsBuilder(clock);
                action(sp, builder);
                var options = sp.GetRequiredService<TempLogOptions>();
                builder.ApplyOptions(options);
                return builder.Build();
            }
        );
    }
}