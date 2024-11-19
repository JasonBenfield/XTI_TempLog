using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using XTI_Core;
using XTI_Core.Extensions;
using XTI_Core.Fakes;
using XTI_TempLog.Abstractions;
using XTI_TempLog.Extensions;
using XTI_TempLog.Fakes;

namespace XTI_TempLog.Tests;

public sealed class ThrottledTempLogSessionTest
{
    [Test]
    public async Task ShouldNotLogRequest_WhenMadeBeforeThrottledInterval()
    {
        var path = "/group1/action 1";
        var throttleInterval = TimeSpan.FromMinutes(1);
        var sp = Setup
        (
            (sp, builder) =>
            {
                builder.Throttle(path).Requests().For(throttleInterval);
            }
        );
        var tempLogSession = sp.GetRequiredService<TempLogSession>();
        await tempLogSession.StartSession();
        await tempLogSession.StartRequest("/Test/Current/group1/action1");
        await tempLogSession.EndRequest();
        var clock = sp.GetRequiredService<FakeClock>();
        clock.Add(throttleInterval.Subtract(TimeSpan.FromSeconds(1)));
        await tempLogSession.StartRequest("/Test/Current/group1/action1");
        var logFiles = await WriteLogFiles(sp);
        var sessionDetails = await logFiles[0].Read();
        var requests = sessionDetails
            .SelectMany(sd => sd.RequestDetails.Select(rd => rd.Request))
            .ToArray();
        Assert.That(requests.Length, Is.EqualTo(1), "Should not log second start request within the throttle interval");
    }

    [Test]
    public async Task ShouldIncrementActualCountOfRequestsLogged()
    {
        var path = "group1/action1";
        var throttleInterval = TimeSpan.FromMinutes(1);
        var sp = Setup
        (
            (_, builder) =>
            {
                builder.Throttle(path).Requests().For(throttleInterval);
            }
        );
        var tempLogSession = sp.GetRequiredService<TempLogSession>();
        await tempLogSession.StartSession();
        await tempLogSession.StartRequest($"Test/Current/{path}");
        await tempLogSession.EndRequest();
        var clock = (FakeClock)sp.GetRequiredService<IClock>();
        clock.Add(throttleInterval.Subtract(TimeSpan.FromSeconds(1)));
        await tempLogSession.StartRequest($"Test/Current/{path}");
        clock.Add(throttleInterval.Subtract(TimeSpan.FromSeconds(2)));
        await tempLogSession.StartRequest($"Test/Current/{path}");
        var logFiles = await WriteLogFiles(sp);
        var sessionDetails = await logFiles[0].Read();
        var requests = sessionDetails
            .SelectMany(sd => sd.RequestDetails.Select(rd => rd.Request))
            .OrderBy(r => r.TimeStarted)
            .ToArray();
        Assert.That(requests[0].ActualCount, Is.EqualTo(1), "Should count the logged request");
        Assert.That(requests[1].ActualCount, Is.EqualTo(2), "Should count the throttled request");
    }

    [Test]
    public async Task ShouldThrottleMultiplePaths()
    {
        var path1 = "group1/action1";
        var path2 = "group1/action2";
        var sp = Setup
        (
            (_, builder) => builder
                .Throttle(path1)
                .Requests().For(2).Minutes()
                .AndThrottle(path2)
                .Requests().For(3).Minutes()
        );
        var tempLogSession = sp.GetRequiredService<TempLogSession>();
        await tempLogSession.StartSession();
        await tempLogSession.StartRequest($"Test/Current/{path1}");
        await tempLogSession.EndRequest();
        await tempLogSession.StartSession();
        await tempLogSession.StartRequest($"Test/Current/{path2}");
        await tempLogSession.EndRequest();
        var clock = (FakeClock)sp.GetRequiredService<IClock>();
        clock.Add(TimeSpan.FromMinutes(1.5));
        await tempLogSession.StartRequest($"Test/Current/{path1}");
        await tempLogSession.StartRequest($"Test/Current/{path2}");
        var logFiles = await WriteLogFiles(sp);
        var sessionDetails = await logFiles[0].Read();
        var requests = sessionDetails
            .SelectMany(sd => sd.RequestDetails.Select(rd => rd.Request))
            .OrderBy(r => r.TimeStarted)
            .ToArray();
        Assert.That(requests.Length, Is.EqualTo(2), "Should not log second start request for either path within the throttle interval");
    }

    [Test]
    public async Task ShouldNotLogStartRequest_WhenMadeBeforeThrottledIntervalForOneMinute()
    {
        var path = "group1/action1";
        var sp = Setup
        (
            (_, builder) =>
            {
                builder.Throttle(path).Requests().ForOneMinute();
            }
        );
        var tempLogSession = sp.GetRequiredService<TempLogSession>();
        await tempLogSession.StartSession();
        await tempLogSession.StartRequest($"Test/Current/{path}");
        await tempLogSession.EndRequest();
        var clock = (FakeClock)sp.GetRequiredService<IClock>();
        clock.Add(TimeSpan.FromSeconds(59));
        await tempLogSession.StartRequest($"Test/Current/{path}");
        var logFiles = await WriteLogFiles(sp);
        var sessionDetails = await logFiles[0].Read();
        var requests = sessionDetails
            .SelectMany(sd => sd.RequestDetails.Select(rd => rd.Request))
            .OrderBy(r => r.TimeStarted)
            .ToArray();
        Assert.That(requests.Length, Is.EqualTo(1), "Should not log second start request within the throttle interval");
    }

    [Test]
    public async Task ShouldNotLogStartRequest_WhenMadeBeforeThrottledIntervalUsingOptions()
    {
        var path = "group1/action1";
        var throttleInterval = TimeSpan.FromMinutes(1);
        var sp = Setup
        (
            (_, builder) =>
            {
                builder.Throttle(path).Requests().For(throttleInterval);
            }
        );
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
            .OrderBy(r => r.TimeStarted)
            .ToArray();
        Assert.That(requests.Length, Is.EqualTo(1), "Should not log second start request within the throttle interval");
    }

    [Test]
    [TestCase("group1/action1"), TestCase("group1/action2")]
    public async Task ShouldThrottlePathBasedOnRegularExpression(string path)
    {
        var throttleInterval = TimeSpan.FromMinutes(1);
        var sp = Setup
        (
            (_, builder) =>
            {
                builder.Throttle(path).Requests().For(throttleInterval);
            }
        );
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
            .OrderBy(r => r.TimeStarted)
            .ToArray();
        Assert.That(requests.Length, Is.EqualTo(1), "Should not log second start request within the throttle interval");
    }

    [Test]
    public async Task ShouldNotLogEndRequest_WhenMadeBeforeThrottledInterval()
    {
        var path = "group1/action1";
        var throttleInterval = TimeSpan.FromMinutes(1);
        var sp = Setup
        (
            (sp, builder) =>
            {
                builder.Throttle(path).Requests().For(throttleInterval);
            }
        );
        var tempLogSession = sp.GetRequiredService<TempLogSession>();
        await tempLogSession.StartSession();
        await tempLogSession.StartRequest(path);
        await tempLogSession.EndRequest();
        var clock = (FakeClock)sp.GetRequiredService<IClock>();
        clock.Add(throttleInterval.Subtract(TimeSpan.FromSeconds(1)));
        await tempLogSession.StartRequest(path);
        await tempLogSession.EndRequest();
        var logFiles = await WriteLogFiles(sp);
        var sessionDetails = await logFiles[0].Read();
        var requests = sessionDetails
            .SelectMany(sd => sd.RequestDetails.Select(rd => rd.Request))
            .OrderBy(r => r.TimeStarted)
            .ToArray();
        Assert.That(requests.Length, Is.EqualTo(1), "Should not log second end request within the throttle interval");
    }

    [Test]
    public async Task ShouldLogStartRequest_WhenPathIsNotThrottled()
    {
        var throttleInterval = TimeSpan.FromMinutes(1);
        var sp = Setup
        (
            (_, builder) => builder
                .Throttle("group1/action1")
                .Requests().For(throttleInterval)
        );
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
            .OrderBy(r => r.TimeStarted)
            .ToArray();
        Assert.That(requests.Length, Is.EqualTo(2), "Should log when path is not throttled");
    }

    [Test]
    public async Task ShouldLogStartRequest_WhenMadeAfterThrottledInterval()
    {
        var path = "group1/action1";
        var throttleInterval = TimeSpan.FromMinutes(1);
        var sp = Setup
        (
            (_, builder) => builder
                .Throttle(path)
                .Requests().For(throttleInterval)
        );
        var tempLogSession = sp.GetRequiredService<TempLogSession>();
        await tempLogSession.StartSession();
        await tempLogSession.StartRequest($"Test/Current/{path}");
        await tempLogSession.EndRequest();
        var clock = sp.GetRequiredService<FakeClock>();
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
            .OrderBy(r => r.TimeStarted)
            .ToArray();
        Assert.That(requests.Length, Is.EqualTo(2), "Should log start request after the throttle interval");
    }

    [Test]
    public async Task ShouldNotLogException_WhenMadeBeforeThrottledInterval()
    {
        var throttleInterval = TimeSpan.FromMinutes(1);
        var path = "group1/action1";
        var sp = Setup
        (
            (_, builder) => builder
                .Throttle(path)
                .Exceptions().For(throttleInterval)
        );
        var tempLogSession = sp.GetRequiredService<TempLogSession>();
        await tempLogSession.StartSession();
        await tempLogSession.StartRequest(path);
        await LogException(tempLogSession);
        await tempLogSession.EndRequest();
        var clock = sp.GetRequiredService<FakeClock>();
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
    public async Task ShouldLogActualCountOfLoggedEvents()
    {
        var throttleInterval = TimeSpan.FromMinutes(1);
        var path = "group1/action1";
        var sp = Setup
        (
            (_, builder) => builder
                .Throttle(path)
                .Exceptions().For(throttleInterval)
        );
        var tempLogSession = sp.GetRequiredService<TempLogSession>();
        await tempLogSession.StartSession();
        await tempLogSession.StartRequest(path);
        await LogException(tempLogSession);
        await tempLogSession.EndRequest();
        var clock = sp.GetRequiredService<FakeClock>();
        clock.Add(throttleInterval.Subtract(TimeSpan.FromSeconds(1)));
        await tempLogSession.StartRequest(path);
        await LogException(tempLogSession);
        await tempLogSession.EndRequest();
        clock.Add(throttleInterval.Add(TimeSpan.FromSeconds(2)));
        await tempLogSession.StartRequest(path);
        await LogException(tempLogSession);
        await tempLogSession.EndRequest();
        var logFiles = await WriteLogFiles(sp);
        var sessionDetails = await logFiles[0].Read();
        var logEntries = sessionDetails
            .SelectMany(sd => sd.RequestDetails.SelectMany(rd => rd.LogEntries))
            .OrderBy(r => r.TimeOccurred)
            .ToArray();
        Assert.That(logEntries[0].ActualCount, Is.EqualTo(1), "Should count the logged exception");
        Assert.That(logEntries[1].ActualCount, Is.EqualTo(2), "Should count the throttled exception");
    }

    [Test]
    public async Task ShouldNotLogException_WhenMadeBeforeThrottledIntervalOfFiveMinutes()
    {
        var path = "group1/action1";
        var sp = Setup
        (
            (_, builder) => builder
                .Throttle(path)
                .Exceptions().For(5).Minutes()
        );
        var tempLogSession = sp.GetRequiredService<TempLogSession>();
        await tempLogSession.StartSession();
        await tempLogSession.StartRequest(path);
        await LogException(tempLogSession);
        await tempLogSession.EndRequest();
        var clock = (FakeClock)sp.GetRequiredService<IClock>();
        clock.Add(TimeSpan.FromMinutes(2));
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
        var sp = Setup
        (
            (_, builder) => builder
                .Throttle(path)
                .Exceptions().For(throttleInterval)
        );
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
        var sp = Setup
        (
            (sp, builder) => builder
                .Throttle(path)
                .Requests().For(throttleInterval)
        );
        var tempLogSession = sp.GetRequiredService<TempLogSession>();
        await tempLogSession.StartSession();
        await tempLogSession.StartRequest(path);
        await tempLogSession.EndRequest();
        var clock = sp.GetRequiredService<FakeClock>();
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
        Assert.That(requests.Length, Is.EqualTo(2), "Should log throttled start request when logging an event");
    }

    [Test]
    public async Task ShouldLogThrottledEndRequest_WhenExceptionIsLogged()
    {
        var throttleInterval = TimeSpan.FromMinutes(1);
        var path = "group1/action1";
        var sp = Setup
        (
            (_, builder) => builder
                .Throttle(path)
                .Requests().For(throttleInterval)
        );
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

    private IServiceProvider Setup(Action<IServiceProvider, ThrottledLogsBuilder> buildThrottledLogs)
    {
        Environment.SetEnvironmentVariable("DOTNET_ENVIRONMENT", "Test");
        var hostBuilder = new XtiHostBuilder();
        hostBuilder.Services.AddMemoryCache();
        hostBuilder.Services.AddFakeTempLogServices();
        hostBuilder.Services.AddThrottledLog(buildThrottledLogs);
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

}