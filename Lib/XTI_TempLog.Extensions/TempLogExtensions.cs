using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using XTI_Core;

namespace XTI_TempLog.Extensions
{
    public static class TempLogExtensions
    {
        public static void AddTempLogServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<TempLogOptions>(configuration.GetSection(TempLogOptions.TempLog));
            services.AddSingleton<ThrottledLogs>();
            services.AddScoped<TempLog>(sp =>
            {
                var dataProtector = sp.GetDataProtector("XTI_TempLog");
                var appDataFolder = sp.GetService<AppDataFolder>();
                return new DiskTempLog(dataProtector, appDataFolder.WithSubFolder("TempLogs").Path());
            });
            services.AddScoped<TempLogSession>();
        }
    }
}
