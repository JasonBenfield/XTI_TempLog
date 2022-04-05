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
    public async Task ShouldNotLogStartRequest_WhenMadeBeforeThrottledInterval()
    {
        var path = "group1/action1";
        var throttleInterval = TimeSpan.FromMinutes(1);
        var services = setup
        (
            (sp, builder) =>
            {
                builder.Throttle(path).Requests().For(throttleInterval);
            }
        );
        var tempLogSession = services.GetRequiredService<TempLogSession>();
        await tempLogSession.StartSession();
        await tempLogSession.StartRequest($"Test/Current/{path}");
        await tempLogSession.EndRequest();
        var clock = (FakeClock)services.GetRequiredService<IClock>();
        clock.Add(throttleInterval.Subtract(TimeSpan.FromSeconds(1)));
        await tempLogSession.StartRequest($"Test/Current/{path}");
        var tempLog = services.GetRequiredService<TempLog>();
        var startRequests = tempLog.StartRequestFiles(clock.Now()).ToArray();
        Assert.That(startRequests.Length, Is.EqualTo(1), "Should not log second start request within the throttle interval");
    }

    [Test]
    public async Task ShouldIncrementActualCountOfRequestsLogged()
    {
        var path = "group1/action1";
        var throttleInterval = TimeSpan.FromMinutes(1);
        var services = setup
        (
            (sp, builder) =>
            {
                builder.Throttle(path).Requests().For(throttleInterval);
            }
        );
        var tempLogSession = services.GetRequiredService<TempLogSession>();
        await tempLogSession.StartSession();
        await tempLogSession.StartRequest($"Test/Current/{path}");
        await tempLogSession.EndRequest();
        var clock = (FakeClock)services.GetRequiredService<IClock>();
        clock.Add(throttleInterval.Subtract(TimeSpan.FromSeconds(1)));
        await tempLogSession.StartRequest($"Test/Current/{path}");
        clock.Add(throttleInterval.Subtract(TimeSpan.FromSeconds(2)));
        await tempLogSession.StartRequest($"Test/Current/{path}");
        var tempLog = services.GetRequiredService<TempLog>();
        var startRequestFiles = tempLog.StartRequestFiles(clock.Now()).ToArray();
        var startRequests = await deserializeStartRequestFiles(startRequestFiles);
        startRequests = startRequests.OrderBy(r => r.TimeStarted).ToArray();
        Assert.That(startRequests[0].ActualCount, Is.EqualTo(1), "Should count the logged request");
        Assert.That(startRequests[1].ActualCount, Is.EqualTo(2), "Should count the throttled request");
    }

    private async Task<StartRequestModel[]> deserializeStartRequestFiles(ITempLogFile[] files)
    {
        var requests = new List<StartRequestModel>();
        foreach (var file in files)
        {
            var serialized = await file.Read();
            requests.Add(XtiSerializer.Deserialize<StartRequestModel>(serialized));
        }
        return requests.ToArray();
    }

    [Test]
    public async Task ShouldThrottleMultiplePaths()
    {
        var path1 = "group1/action1";
        var path2 = "group1/action2";
        var services = setup
        (
            (sp, builder) => builder
                .Throttle(path1)
                .Requests().For(2).Minutes()
                .AndThrottle(path2)
                .Requests().For(3).Minutes()
        );
        var tempLogSession = services.GetRequiredService<TempLogSession>();
        await tempLogSession.StartSession();
        await tempLogSession.StartRequest($"Test/Current/{path1}");
        await tempLogSession.EndRequest();
        await tempLogSession.StartSession();
        await tempLogSession.StartRequest($"Test/Current/{path2}");
        await tempLogSession.EndRequest();
        var clock = (FakeClock)services.GetRequiredService<IClock>();
        clock.Add(TimeSpan.FromMinutes(1.5));
        await tempLogSession.StartRequest($"Test/Current/{path1}");
        await tempLogSession.StartRequest($"Test/Current/{path2}");
        var tempLog = services.GetRequiredService<TempLog>();
        var startRequests = tempLog.StartRequestFiles(clock.Now()).ToArray();
        Assert.That(startRequests.Length, Is.EqualTo(2), "Should not log second start request for either path within the throttle interval");
    }

    [Test]
    public async Task ShouldNotLogStartRequest_WhenMadeBeforeThrottledIntervalForOneMinute()
    {
        var path = "group1/action1";
        var services = setup
        (
            (sp, builder) =>
            {
                builder.Throttle(path).Requests().ForOneMinute();
            }
        );
        var tempLogSession = services.GetRequiredService<TempLogSession>();
        await tempLogSession.StartSession();
        await tempLogSession.StartRequest($"Test/Current/{path}");
        await tempLogSession.EndRequest();
        var clock = (FakeClock)services.GetRequiredService<IClock>();
        clock.Add(TimeSpan.FromSeconds(59));
        await tempLogSession.StartRequest($"Test/Current/{path}");
        var tempLog = services.GetRequiredService<TempLog>();
        var startRequests = tempLog.StartRequestFiles(clock.Now()).ToArray();
        Assert.That(startRequests.Length, Is.EqualTo(1), "Should not log second start request within the throttle interval");
    }

    [Test]
    public async Task ShouldNotLogStartRequest_WhenMadeBeforeThrottledIntervalUsingOptions()
    {
        var path = "group1/action1";
        var throttleInterval = TimeSpan.FromMinutes(1);
        var services = setup
        (
            (sp, builder) =>
            {
                builder.Throttle(path).Requests().For(throttleInterval);
            }
        );
        var tempLogSession = services.GetRequiredService<TempLogSession>();
        await tempLogSession.StartSession();
        await tempLogSession.StartRequest($"Test/Current/{path}");
        await tempLogSession.EndRequest();
        var clock = (FakeClock)services.GetRequiredService<IClock>();
        clock.Add(throttleInterval.Subtract(TimeSpan.FromSeconds(1)));
        await tempLogSession.StartRequest($"Test/Current/{path}");
        var tempLog = services.GetRequiredService<TempLog>();
        var startRequests = tempLog.StartRequestFiles(clock.Now()).ToArray();
        Assert.That(startRequests.Length, Is.EqualTo(1), "Should not log second start request within the throttle interval");
    }

    [Test]
    [TestCase("group1/action1"), TestCase("group1/action2")]
    public async Task ShouldThrottlePathBasedOnRegularExpression(string path)
    {
        var throttleInterval = TimeSpan.FromMinutes(1);
        var services = setup
        (
            (sp, builder) =>
            {
                builder.Throttle(path).Requests().For(throttleInterval);
            }
        );
        var tempLogSession = services.GetRequiredService<TempLogSession>();
        await tempLogSession.StartSession();
        await tempLogSession.StartRequest($"Test/Current/{path}");
        await tempLogSession.EndRequest();
        var clock = (FakeClock)services.GetRequiredService<IClock>();
        clock.Add(throttleInterval.Subtract(TimeSpan.FromSeconds(1)));
        await tempLogSession.StartRequest($"Test/Current/{path}");
        var tempLog = services.GetRequiredService<TempLog>();
        var startRequests = tempLog.StartRequestFiles(clock.Now()).ToArray();
        Assert.That(startRequests.Length, Is.EqualTo(1), "Should not log second start request within the throttle interval");
    }

    [Test]
    public async Task ShouldNotLogEndRequest_WhenMadeBeforeThrottledInterval()
    {
        var path = "group1/action1";
        var throttleInterval = TimeSpan.FromMinutes(1);
        var services = setup
        (
            (sp, builder) =>
            {
                builder.Throttle(path).Requests().For(throttleInterval);
            }
        );
        var tempLogSession = services.GetRequiredService<TempLogSession>();
        await tempLogSession.StartSession();
        await tempLogSession.StartRequest(path);
        await tempLogSession.EndRequest();
        var clock = (FakeClock)services.GetRequiredService<IClock>();
        clock.Add(throttleInterval.Subtract(TimeSpan.FromSeconds(1)));
        await tempLogSession.StartRequest(path);
        await tempLogSession.EndRequest();
        var tempLog = services.GetRequiredService<TempLog>();
        var endRequestFiles = tempLog.EndRequestFiles(clock.Now()).ToArray();
        Assert.That(endRequestFiles.Length, Is.EqualTo(1), "Should not log second end request within the throttle interval");
    }

    [Test]
    public async Task ShouldLogStartRequest_WhenPathIsNotThrottled()
    {
        var throttleInterval = TimeSpan.FromMinutes(1);
        var services = setup
        (
            (sp, builder) => builder
                .Throttle("group1/action1")
                .Requests().For(throttleInterval)
        );
        var anotherPath = "group1/action2";
        var tempLogSession = services.GetRequiredService<TempLogSession>();
        await tempLogSession.StartSession();
        await tempLogSession.StartRequest(anotherPath);
        await tempLogSession.EndRequest();
        var clock = (FakeClock)services.GetRequiredService<IClock>();
        clock.Add(throttleInterval.Subtract(TimeSpan.FromSeconds(1)));
        await tempLogSession.StartRequest(anotherPath);
        var tempLog = services.GetRequiredService<TempLog>();
        var startRequests = tempLog.StartRequestFiles(clock.Now()).ToArray();
        Assert.That(startRequests.Length, Is.EqualTo(2), "Should log when path is not throttled");
    }

    [Test]
    public async Task ShouldLogStartRequest_WhenMadeAfterThrottledInterval()
    {
        var path = "group1/action1";
        var throttleInterval = TimeSpan.FromMinutes(1);
        var services = setup
        (
            (sp, builder) => builder
                .Throttle(path)
                .Requests().For(throttleInterval)
        );
        var tempLogSession = services.GetRequiredService<TempLogSession>();
        await tempLogSession.StartSession();
        await tempLogSession.StartRequest($"Test/Current/{path}");
        await tempLogSession.EndRequest();
        var clock = (FakeClock)services.GetRequiredService<IClock>();
        clock.Add(throttleInterval.Subtract(TimeSpan.FromSeconds(1)));
        await tempLogSession.StartRequest($"Test/Current/{path}");
        await tempLogSession.EndRequest();
        clock.Add(TimeSpan.FromSeconds(2));
        await tempLogSession.StartRequest($"Test/Current/{path}");
        await tempLogSession.EndRequest();
        var tempLog = services.GetRequiredService<TempLog>();
        var startRequests = tempLog.StartRequestFiles(clock.Now()).ToArray();
        Assert.That(startRequests.Length, Is.EqualTo(2), "Should log start request after the throttle interval");
    }

    [Test]
    public async Task ShouldNotLogException_WhenMadeBeforeThrottledInterval()
    {
        var throttleInterval = TimeSpan.FromMinutes(1);
        var path = "group1/action1";
        var services = setup
        (
            (sp, builder) => builder
                .Throttle(path)
                .Exceptions().For(throttleInterval)
        );
        var tempLogSession = services.GetRequiredService<TempLogSession>();
        await tempLogSession.StartSession();
        await tempLogSession.StartRequest(path);
        await logException(tempLogSession);
        await tempLogSession.EndRequest();
        var clock = (FakeClock)services.GetRequiredService<IClock>();
        clock.Add(throttleInterval.Subtract(TimeSpan.FromSeconds(1)));
        await tempLogSession.StartRequest(path);
        await logException(tempLogSession);
        await tempLogSession.EndRequest();
        var tempLog = services.GetRequiredService<TempLog>();
        var logEventFiles = tempLog.LogEventFiles(clock.Now()).ToArray();
        Assert.That(logEventFiles.Length, Is.EqualTo(1), "Should not log second event when it happens before the throttle interval");
    }

    [Test]
    public async Task ShouldLogActualCountOfLoggedEvents()
    {
        var throttleInterval = TimeSpan.FromMinutes(1);
        var path = "group1/action1";
        var services = setup
        (
            (sp, builder) => builder
                .Throttle(path)
                .Exceptions().For(throttleInterval)
        );
        var tempLogSession = services.GetRequiredService<TempLogSession>();
        await tempLogSession.StartSession();
        await tempLogSession.StartRequest(path);
        await logException(tempLogSession);
        await tempLogSession.EndRequest();
        var clock = (FakeClock)services.GetRequiredService<IClock>();
        clock.Add(throttleInterval.Subtract(TimeSpan.FromSeconds(1)));
        await tempLogSession.StartRequest(path);
        await logException(tempLogSession);
        await tempLogSession.EndRequest();
        clock.Add(throttleInterval.Add(TimeSpan.FromSeconds(2)));
        await tempLogSession.StartRequest(path);
        await logException(tempLogSession);
        await tempLogSession.EndRequest();
        var tempLog = services.GetRequiredService<TempLog>();
        var logEventFiles = tempLog.LogEventFiles(clock.Now()).ToArray();
        var logEventModels = await deserializeEventFiles(logEventFiles);
        Assert.That(logEventModels[0].ActualCount, Is.EqualTo(1), "Should count the logged exception");
        Assert.That(logEventModels[1].ActualCount, Is.EqualTo(2), "Should count the throttled exception");
    }

    private async Task<LogEventModel[]> deserializeEventFiles(ITempLogFile[] files)
    {
        var eventModels = new List<LogEventModel>();
        foreach (var file in files)
        {
            var serialized = await file.Read();
            eventModels.Add(XtiSerializer.Deserialize<LogEventModel>(serialized));
        }
        return eventModels.OrderBy(e => e.TimeOccurred).ToArray();
    }

    [Test]
    public async Task ShouldNotLogException_WhenMadeBeforeThrottledIntervalOfFiveMinutes()
    {
        var path = "group1/action1";
        var services = setup
        (
            (sp, builder) => builder
                .Throttle(path)
                .Exceptions().For(5).Minutes()
        );
        var tempLogSession = services.GetRequiredService<TempLogSession>();
        await tempLogSession.StartSession();
        await tempLogSession.StartRequest(path);
        await logException(tempLogSession);
        await tempLogSession.EndRequest();
        var clock = (FakeClock)services.GetRequiredService<IClock>();
        clock.Add(TimeSpan.FromMinutes(2));
        await tempLogSession.StartRequest(path);
        await logException(tempLogSession);
        await tempLogSession.EndRequest();
        var tempLog = services.GetRequiredService<TempLog>();
        var logEventFiles = tempLog.LogEventFiles(clock.Now()).ToArray();
        Assert.That(logEventFiles.Length, Is.EqualTo(1), "Should not log second event when it happens before the throttle interval");
    }

    [Test]
    public async Task ShouldLogException_WhenMadeAfterThrottledInterval()
    {
        var path = "group1/action1";
        var throttleInterval = TimeSpan.FromMinutes(1);
        var services = setup
        (
            (sp, builder) => builder
                .Throttle(path)
                .Exceptions().For(throttleInterval)
        );
        var tempLogSession = services.GetRequiredService<TempLogSession>();
        await tempLogSession.StartSession();
        await tempLogSession.StartRequest($"Test/Current/{path}");
        await logException(tempLogSession);
        await tempLogSession.EndRequest();
        var clock = (FakeClock)services.GetRequiredService<IClock>();
        clock.Add(throttleInterval.Subtract(TimeSpan.FromSeconds(1)));
        await tempLogSession.StartRequest($"Test/Current/{path}");
        await logException(tempLogSession);
        await tempLogSession.EndRequest();
        clock.Add(TimeSpan.FromSeconds(2));
        await tempLogSession.StartRequest($"Test/Current/{path}");
        await logException(tempLogSession);
        await tempLogSession.EndRequest();
        var tempLog = services.GetRequiredService<TempLog>();
        var logEventFiles = tempLog.LogEventFiles(clock.Now()).ToArray();
        Assert.That(logEventFiles.Length, Is.EqualTo(2), "Should log exception after the throttle interval");
    }

    [Test]
    public async Task ShouldLogThrottledStartRequest_WhenExceptionIsLogged()
    {
        var throttleInterval = TimeSpan.FromMinutes(1);
        var path = "group1/action1";
        var services = setup
        (
            (sp, builder) => builder
                .Throttle(path)
                .Requests().For(throttleInterval)
        );
        var tempLogSession = services.GetRequiredService<TempLogSession>();
        await tempLogSession.StartSession();
        await tempLogSession.StartRequest(path);
        await tempLogSession.EndRequest();
        var clock = (FakeClock)services.GetRequiredService<IClock>();
        clock.Add(throttleInterval.Subtract(TimeSpan.FromSeconds(1)));
        await tempLogSession.StartRequest(path);
        await logException(tempLogSession);
        await tempLogSession.EndRequest();
        var tempLog = services.GetRequiredService<TempLog>();
        var startRequests = tempLog.StartRequestFiles(clock.Now()).ToArray();
        Assert.That(startRequests.Length, Is.EqualTo(2), "Should log throttled start request when logging an event");
    }

    [Test]
    public async Task ShouldLogThrottledEndRequest_WhenExceptionIsLogged()
    {
        var throttleInterval = TimeSpan.FromMinutes(1);
        var path = "group1/action1";
        var services = setup
        (
            (sp, builder) => builder
                .Throttle(path)
                .Requests().For(throttleInterval)
        );
        var tempLogSession = services.GetRequiredService<TempLogSession>();
        await tempLogSession.StartSession();
        await tempLogSession.StartRequest(path);
        await tempLogSession.EndRequest();
        var clock = (FakeClock)services.GetRequiredService<IClock>();
        clock.Add(throttleInterval.Subtract(TimeSpan.FromSeconds(1)));
        await tempLogSession.StartRequest(path);
        await logException(tempLogSession);
        await tempLogSession.EndRequest();
        var tempLog = services.GetRequiredService<TempLog>();
        var endRequests = tempLog.EndRequestFiles(clock.Now()).ToArray();
        Assert.That(endRequests.Length, Is.EqualTo(2), "Should log throttled end request when logging an event");
    }

    private static async Task logException(TempLogSession tempLogSession)
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
                "An unexpected error occurred"
            );
        }
    }

    private IServiceProvider setup(Action<IServiceProvider, ThrottledLogsBuilder> buildThrottledLogs)
    {
        var hostBuilder = new XtiHostBuilder();
        hostBuilder.Services.AddFakeTempLogServices();
        hostBuilder.Services.AddThrottledLog(buildThrottledLogs);
        hostBuilder.Services.AddSingleton<IClock, FakeClock>();
        hostBuilder.Services.AddScoped<IAppEnvironmentContext>(sp => new FakeAppEnvironmentContext
        {
            Environment = new AppEnvironment
            (
                "test.user", "my-computer", "10.1.0.0", "Windows 10", "WebApp"
            )
        });
        var host = hostBuilder.Build();
        var env = host.GetRequiredService<XtiEnvironmentAccessor>();
        env.UseTest();
        return host.Scope();
    }
}