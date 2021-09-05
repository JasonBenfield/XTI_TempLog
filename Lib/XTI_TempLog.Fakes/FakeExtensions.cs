using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using XTI_Core.Fakes;

namespace XTI_TempLog.Fakes
{
    public static class FakeExtensions
    {
        public static void AddFakeTempLogServices(this IServiceCollection services)
        {
            services.AddSingleton<CurrentSession>();
            services.AddSingleton<TempLog, FakeTempLog>();
            services.AddSingleton<IOptions<TempLogOptions>, FakeOptions<TempLogOptions>>();
            services.AddSingleton<ThrottledLogs>();
            services.AddScoped<TempLogSession>();
        }
    }
}
