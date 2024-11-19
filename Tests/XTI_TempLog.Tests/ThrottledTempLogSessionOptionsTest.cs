using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using XTI_Core;
using XTI_Core.Extensions;
using XTI_Core.Fakes;
using XTI_TempLog.Abstractions;
using XTI_TempLog.Fakes;

namespace XTI_TempLog.Tests;

internal sealed class ThrottledTempLogSessionOptionsTest
{
    [Test]
    public async Task ShouldNotLogStartRequest_WhenMadeBeforeThrottledInterval()
    {
        var path = "group1/action1";
        var throttleInterval = TimeSpan.FromMinutes(1);
        var throttles = new[]
        {
            new TempLogThrottleOptions
            {
                Path = path,
                ThrottleRequestInterval = (int)throttleInterval.TotalMilliseconds
            }
        };
        var sp = Setup(throttles);
        var tempLogSession = sp.GetRequiredService<TempLogSession>();
        await tempLogSession.StartSession();
        await tempLogSession.StartRequest($"Test/Current/{path}");
        await tempLogSession.EndRequest();
        var clock = (FakeClock)sp.GetRequiredService<IClock>();
        clock.Add(throttleInterval.Subtract(TimeSpan.FromSeconds(1)));
        await tempLogSession.StartRequest($"Test/Current/{path}");
        var logFiles = await WriteLogFiles(sp);
        var sessionDetails = await logFiles[0].Read();
        var requests = sessionDetails
            .SelectMany(sd => sd.RequestDetails.Select(rd => rd.Request))
            .ToArray();
        Assert.That(requests.Length, Is.EqualTo(1), "Should not log second start request within the throttle interval");
    }

    [Test]
    public async Task ShouldNotLogStartRequest_WhenMadeBeforeThrottledIntervalUsingOptions()
    {
        var path = "group1/action1";
        var throttleInterval = TimeSpan.FromMinutes(1);
        var throttles = new[]
        {
            new TempLogThrottleOptions
            {
                Path = path,
                ThrottleRequestInterval = (int)throttleInterval.TotalMilliseconds
            }
        };
        var sp = Setup(throttles);
        var tempLogSession = sp.GetRequiredService<TempLogSession>();
        await tempLogSession.StartSession();
        await tempLogSession.StartRequest($"Test/Current/{path}");
        await tempLogSession.EndRequest();
        var clock = sp.GetRequiredService<FakeClock>();
        clock.Add(throttleInterval.Subtract(TimeSpan.FromSeconds(1)));
        await tempLogSession.StartRequest($"Test/Current/{path}");
        var logFiles = await WriteLogFiles(sp);
        var sessionDetails = await logFiles[0].Read();
        var requests = sessionDetails
            .SelectMany(sd => sd.RequestDetails.Select(rd => rd.Request))
            .ToArray();
        Assert.That(requests.Length, Is.EqualTo(1), "Should not log second start request within the throttle interval");
    }

    [Test]
    [TestCase("group1/action1"), TestCase("group1/action2")]
    public async Task ShouldThrottlePathBasedOnRegularExpression(string path)
    {
        var throttleInterval = TimeSpan.FromMinutes(1);
        var throttles = new[]
        {
            new TempLogThrottleOptions
            {
                Path = "group1/action\\d+$",
                ThrottleRequestInterval = (int)throttleInterval.TotalMilliseconds
            }
        };
        var sp = Setup(throttles);
        var tempLogSession = sp.GetRequiredService<TempLogSession>();
        await tempLogSession.StartSession();
        await tempLogSession.StartRequest($"Test/Current/{path}");
        await tempLogSession.EndRequest();
        var clock = sp.GetRequiredService<FakeClock>();
        clock.Add(throttleInterval.Subtract(TimeSpan.FromSeconds(1)));
        await tempLogSession.StartRequest($"Test/Current/{path}");
        var logFiles = await WriteLogFiles(sp);
        var sessionDetails = await logFiles[0].Read();
        var requests = sessionDetails
            .SelectMany(sd => sd.RequestDetails.Select(rd => rd.Request))
            .ToArray();
        Assert.That(requests.Length, Is.EqualTo(1), "Should not log second start request within the throttle interval");
    }

    [Test]
    public async Task ShouldLogStartRequest_WhenPathIsNotThrottled()
    {
        var throttleInterval = TimeSpan.FromMinutes(1);
        var throttles = new[]
        {
            new TempLogThrottleOptions
            {
                Path = "group1/action1",
                ThrottleRequestInterval = (int)throttleInterval.TotalMilliseconds
            }
        };
        var sp = Setup(throttles);
        var anotherPath = "group1/action2";
        var tempLogSession = sp.GetRequiredService<TempLogSession>();
        await tempLogSession.StartSession();
        await tempLogSession.StartRequest(anotherPath);
        await tempLogSession.EndRequest();
        var clock = sp.GetRequiredService<FakeClock>();
        clock.Add(throttleInterval.Subtract(TimeSpan.FromSeconds(1)));
        await tempLogSession.StartRequest(anotherPath);
        var logFiles = await WriteLogFiles(sp);
        var sessionDetails = await logFiles[0].Read();
        var requests = sessionDetails
            .SelectMany(sd => sd.RequestDetails.Select(rd => rd.Request))
            .ToArray();
        Assert.That(requests.Length, Is.EqualTo(2), "Should log when path is not throttled");
    }

    [Test]
    public async Task ShouldLogStartRequest_WhenMadeAfterThrottledInterval()
    {
        var path = "group1/action1";
        var throttleInterval = TimeSpan.FromMinutes(1);
        var throttles = new[]
        {
            new TempLogThrottleOptions
            {
                Path = path,
                ThrottleRequestInterval = (int)throttleInterval.TotalMilliseconds
            }
        };
        var sp = Setup(throttles);
        var tempLogSession = sp.GetRequiredService<TempLogSession>();
        await tempLogSession.StartSession();
        await tempLogSession.StartRequest($"Test/Current/{path}");
        await tempLogSession.EndRequest();
        var clock = (FakeClock)sp.GetRequiredService<IClock>();
        clock.Add(throttleInterval.Subtract(TimeSpan.FromSeconds(1)));
        await tempLogSession.StartRequest($"Test/Current/{path}");
        await tempLogSession.EndRequest();
        clock.Add(TimeSpan.FromSeconds(2));
        await tempLogSession.StartRequest($"Test/Current/{path}");
        await tempLogSession.EndRequest();
        var logFiles = await WriteLogFiles(sp);
        var sessionDetails = await logFiles[0].Read();
        var requests = sessionDetails
            .SelectMany(sd => sd.RequestDetails.Select(rd => rd.Request))
            .ToArray();
        Assert.That(requests.Length, Is.EqualTo(2), "Should log start request after the throttle interval");
    }

    [Test]
    public async Task ShouldNotLogException_WhenMadeBeforeThrottledInterval()
    {
        var throttleInterval = TimeSpan.FromMinutes(1);
        var path = "group1/action1";
        var throttles = new[]
        {
            new TempLogThrottleOptions
            {
                Path = path,
                ThrottleExceptionInterval = (int)throttleInterval.TotalMilliseconds
            }
        };
        var sp = Setup(throttles);
        var tempLogSession = sp.GetRequiredService<TempLogSession>();
        await tempLogSession.StartSession();
        await tempLogSession.StartRequest(path);
        await LogException(tempLogSession);
        await tempLogSession.EndRequest();
        var clock = (FakeClock)sp.GetRequiredService<IClock>();
        clock.Add(throttleInterval.Subtract(TimeSpan.FromSeconds(1)));
        await tempLogSession.StartRequest(path);
        await LogException(tempLogSession);
        await tempLogSession.EndRequest();
        var logFiles = await WriteLogFiles(sp);
        var sessionDetails = await logFiles[0].Read();
        var logEntries = sessionDetails
            .SelectMany(sd => sd.RequestDetails.SelectMany(rd => rd.LogEntries))
            .OrderBy(r => r.TimeOccurred)
            .ToArray();
        Assert.That(logEntries.Length, Is.EqualTo(1), "Should not log second event when it happens before the throttle interval");
    }

    [Test]
    public async Task ShouldLogException_WhenMadeAfterThrottledInterval()
    {
        var path = "group1/action1";
        var throttleInterval = TimeSpan.FromMinutes(1);
        var throttles = new[]
        {
            new TempLogThrottleOptions
            {
                Path = path,
                ThrottleExceptionInterval = (int)throttleInterval.TotalMilliseconds
            }
        };
        var sp = Setup(throttles);
        var tempLogSession = sp.GetRequiredService<TempLogSession>();
        await tempLogSession.StartSession();
        await tempLogSession.StartRequest($"Test/Current/{path}");
        await LogException(tempLogSession);
        await tempLogSession.EndRequest();
        var clock = (FakeClock)sp.GetRequiredService<IClock>();
        clock.Add(throttleInterval.Subtract(TimeSpan.FromSeconds(1)));
        await tempLogSession.StartRequest($"Test/Current/{path}");
        await LogException(tempLogSession);
        await tempLogSession.EndRequest();
        clock.Add(TimeSpan.FromSeconds(2));
        await tempLogSession.StartRequest($"Test/Current/{path}");
        await LogException(tempLogSession);
        await tempLogSession.EndRequest();
        var logFiles = await WriteLogFiles(sp);
        var sessionDetails = await logFiles[0].Read();
        var logEntries = sessionDetails
            .SelectMany(sd => sd.RequestDetails.SelectMany(rd => rd.LogEntries))
            .OrderBy(r => r.TimeOccurred)
            .ToArray();
        Assert.That(logEntries.Length, Is.EqualTo(2), "Should log exception after the throttle interval");
    }

    [Test]
    public async Task ShouldLogThrottledStartRequest_WhenExceptionIsLogged()
    {
        var throttleInterval = TimeSpan.FromMinutes(1);
        var path = "group1/action1";
        var throttles = new[]
        {
            new TempLogThrottleOptions
            {
                Path = path,
                ThrottleRequestInterval = (int)throttleInterval.TotalMilliseconds
            }
        };
        var sp = Setup(throttles);
        var tempLogSession = sp.GetRequiredService<TempLogSession>();
        await tempLogSession.StartSession();
        await tempLogSession.StartRequest(path);
        await tempLogSession.EndRequest();
        var clock = (FakeClock)sp.GetRequiredService<IClock>();
        clock.Add(throttleInterval.Subtract(TimeSpan.FromSeconds(1)));
        await tempLogSession.StartRequest(path);
        await LogException(tempLogSession);
        await tempLogSession.EndRequest();
        var logFiles = await WriteLogFiles(sp);
        var sessionDetails = await logFiles[0].Read();
        var requests = sessionDetails
            .SelectMany(sd => sd.RequestDetails.Select(rd => rd.Request))
            .ToArray();
        Assert.That(requests.Length, Is.EqualTo(2), "Should log throttled start request when logging an event");
    }

    [Test]
    public async Task ShouldLogThrottledEndRequest_WhenExceptionIsLogged()
    {
        var throttleInterval = TimeSpan.FromMinutes(1);
        var path = "group1/action1";
        var throttles = new[]
        {
            new TempLogThrottleOptions
            {
                Path = path,
                ThrottleRequestInterval = (int)throttleInterval.TotalMilliseconds
            }
        };
        var sp = Setup(throttles);
        var tempLogSession = sp.GetRequiredService<TempLogSession>();
        await tempLogSession.StartSession();
        await tempLogSession.StartRequest(path);
        await tempLogSession.EndRequest();
        var clock = (FakeClock)sp.GetRequiredService<IClock>();
        clock.Add(throttleInterval.Subtract(TimeSpan.FromSeconds(1)));
        await tempLogSession.StartRequest(path);
        await LogException(tempLogSession);
        await tempLogSession.EndRequest();
        var logFiles = await WriteLogFiles(sp);
        var sessionDetails = await logFiles[0].Read();
        var requests = sessionDetails
            .SelectMany(sd => sd.RequestDetails.Select(rd => rd.Request))
            .OrderBy(r => r.TimeStarted)
            .ToArray();
        Assert.That(requests.Length, Is.EqualTo(2), "Should log throttled end request when logging an event");
    }

    private static async Task<ITempLogFile[]> WriteLogFiles(IServiceProvider sp)
    {
        var tempLogRepo = sp.GetRequiredService<TempLogRepository>();
        await tempLogRepo.WriteToLocalStorage();
        var clock = sp.GetRequiredService<IClock>();
        var tempLog = sp.GetRequiredService<TempLog>();
        var logFiles = tempLog.Files(clock.Now().AddSeconds(1), 100);
        return logFiles;
    }

    private static async Task LogException(TempLogSession tempLogSession)
    {
        try
        {
            throw new Exception("Test");
        }
        catch (Exception ex)
        {
            await tempLogSession.LogException
            (
                AppEventSeverity.Values.CriticalError,
                ex,
                "An unexpected error occurred",
                ""
            );
        }
    }

    private IServiceProvider Setup(IEnumerable<TempLogThrottleOptions> throttles)
    {
        Environment.SetEnvironmentVariable("DOTNET_ENVIRONMENT", "Test");
        var hostBuilder = new XtiHostBuilder();
        hostBuilder.Services.AddMemoryCache();
        hostBuilder.Services.AddFakeTempLogServices();
        hostBuilder.Services.AddSingleton
        (
            _ => new TempLogOptions
            {
                Throttles = throttles.ToArray()
            }
        );
        hostBuilder.Services.AddSingleton
        (
            sp =>
            {
                var builder = new ThrottledLogsBuilder(sp.GetRequiredService<IClock>());
                builder.ApplyOptions(sp.GetRequiredService<TempLogOptions>());
                return builder;
            }
        );
        hostBuilder.Services.AddSingleton<FakeClock>();
        hostBuilder.Services.AddSingleton<IClock>(sp => sp.GetRequiredService<FakeClock>());
        hostBuilder.Services.AddScoped<IAppEnvironmentContext>(sp => new FakeAppEnvironmentContext
        {
            Environment = new AppEnvironment
            (
                "test.user", "my-computer", "10.1.0.0", "Windows 10", 123
            )
        });
        var host = hostBuilder.Build();
        return host.Scope();
    }
}