using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using XTI_Core;

namespace XTI_TempLog.Extensions
{
    public static class TempLogExtensions
    {
        public static void AddTempLogServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<TempLogOptions>(configuration.GetSection(TempLogOptions.TempLog));
            services.AddThrottledLog((sp, b) => { });
            services.AddScoped<TempLog>(sp =>
            {
                var dataProtector = sp.GetDataProtector("XTI_TempLog");
                var appDataFolder = sp.GetService<AppDataFolder>();
                return new DiskTempLog(dataProtector, appDataFolder.WithSubFolder("TempLogs").Path());
            });
            services.AddScoped<TempLogSession>();
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
                    var clock = sp.GetService<Clock>();
                    var builder = new ThrottledLogsBuilder(clock);
                    action(sp, builder);
                    var options = sp.GetService<IOptions<TempLogOptions>>().Value;
                    builder.ApplyOptions(options);
                    return builder.Build();
                }
            );
        }
    }
}
