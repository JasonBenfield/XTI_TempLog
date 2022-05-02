using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using XTI_Core;
using XTI_Core.Extensions;
using XTI_Core.Fakes;
using XTI_TempLog.Abstractions;
using XTI_TempLog.Fakes;

namespace XTI_TempLog.Tests;

internal sealed class TempLogSessionTest
{
    [Test]
    public async Task ShouldWriteSessionToLog_WhenStartingASession()
    {
        var services = setup();
        var tempLogSession = getTempLogSession(services);
        await tempLogSession.StartSession();
        var startSession = await getSingleStartSession(services);
        Assert.That(string.IsNullOrWhiteSpace(startSession.SessionKey), Is.False, "Should create session key");
        var clock = getClock(services);
        Assert.That(startSession.TimeStarted, Is.EqualTo(clock.Now()), "Should start session");
        var appEnvironmentContext = getAppEnvironmentContext(services);
        Assert.That(startSession.UserName, Is.EqualTo(appEnvironmentContext.Environment.UserName), "Should set user name from environment");
        Assert.That(startSession.RequesterKey, Is.EqualTo(appEnvironmentContext.Environment.RequesterKey), "Should set requester key from environment");
        Assert.That(startSession.UserAgent, Is.EqualTo(appEnvironmentContext.Environment.UserAgent), "Should set user agent from environment");
        Assert.That(startSession.RemoteAddress, Is.EqualTo(appEnvironmentContext.Environment.RemoteAddress), "Should set remote address from environment");
    }

    private async Task<StartSessionModel> getSingleStartSession(IServiceProvider services)
    {
        var tempLog = getTempLog(services);
        var files = tempLog.StartSessionFiles(DateTime.Now).ToArray();
        Assert.That(files.Length, Is.EqualTo(1), "Should be one start session file");
        var serializedStartSession = await files[0].Read();
        return XtiSerializer.Deserialize<StartSessionModel>(serializedStartSession);
    }

    [Test]
    public async Task ShouldWriteRequestToLog_WhenStartingARequest()
    {
        var services = setup();
        var tempLogSession = getTempLogSession(services);
        await tempLogSession.StartSession();
        var path = "group1/action1";
        await tempLogSession.StartRequest(path);
        var startSession = await getSingleStartSession(services);
        var startRequest = await getSingleStartRequest(services);
        Assert.That(startRequest.SessionKey, Is.EqualTo(startSession.SessionKey), "Should create session key");
        var clock = getClock(services);
        Assert.That(startRequest.TimeStarted, Is.EqualTo(clock.Now()), "Should start session");
        Assert.That(startRequest.Path, Is.EqualTo(path), "Should set path");
        Assert.That(string.IsNullOrWhiteSpace(startRequest.RequestKey), Is.False, "Should set request key");
        Assert.That(startRequest.AppType, Is.EqualTo("WebApp"), "Should set app key from environment");
    }

    private async Task<StartRequestModel> getSingleStartRequest(IServiceProvider services)
    {
        var tempLog = getTempLog(services);
        var files = tempLog.StartRequestFiles(DateTime.Now).ToArray();
        Assert.That(files.Length, Is.EqualTo(1), "Should be one start request file");
        var serializedStartRequest = await files[0].Read();
        return XtiSerializer.Deserialize<StartRequestModel>(serializedStartRequest);
    }

    [Test]
    public async Task ShouldWriteRequestToLog_WhenEndingARequest()
    {
        var services = setup();
        var tempLogSession = getTempLogSession(services);
        await tempLogSession.StartSession();
        var path = "group1/action1";
        await tempLogSession.StartRequest(path);
        var clock = getClock(services);
        clock.Add(TimeSpan.FromMinutes(1));
        await tempLogSession.EndRequest();
        var endRequest = await getSingleEndRequest(services);
        var startRequest = await getSingleStartRequest(services);
        Assert.That(endRequest.RequestKey, Is.EqualTo(startRequest.RequestKey), "Request key should be the same as the start request");
        Assert.That(endRequest.TimeEnded, Is.EqualTo(clock.Now()), "Should set the end time");
    }

    private async Task<EndRequestModel> getSingleEndRequest(IServiceProvider services)
    {
        var tempLog = getTempLog(services);
        var clock = getClock(services);
        var files = tempLog.EndRequestFiles(clock.Now()).ToArray();
        Assert.That(files.Length, Is.EqualTo(1), "Should be one end request file");
        var serializedEndRequest = await files[0].Read();
        return XtiSerializer.Deserialize<EndRequestModel>(serializedEndRequest);
    }

    [Test]
    public async Task ShouldWriteSessionToLog_WhenEndingASession()
    {
        var services = setup();
        var tempLogSession = getTempLogSession(services);
        await tempLogSession.StartSession();
        var path = "group1/action1";
        await tempLogSession.StartRequest(path);
        var clock = getClock(services);
        clock.Add(TimeSpan.FromMinutes(1));
        await tempLogSession.EndRequest();
        await tempLogSession.EndSession();
        var endSession = await getSingleEndSession(services);
        var startSession = await getSingleStartSession(services);
        Assert.That(endSession.SessionKey, Is.EqualTo(startSession.SessionKey), "Should have the same session key as the start session");
        Assert.That(endSession.TimeEnded, Is.EqualTo(clock.Now()), "Should set time ended");
    }

    private async Task<EndSessionModel> getSingleEndSession(IServiceProvider services)
    {
        var tempLog = getTempLog(services);
        var clock = getClock(services);
        var files = tempLog.EndSessionFiles(clock.Now()).ToArray();
        Assert.That(files.Length, Is.EqualTo(1), "Should be one end session file");
        var serializedEndSession = await files[0].Read();
        return XtiSerializer.Deserialize<EndSessionModel>(serializedEndSession);
    }

    [Test]
    public async Task ShouldAuthenticateSession()
    {
        var services = setup();
        var tempLogSession = getTempLogSession(services);
        await tempLogSession.StartSession();
        var userName = "test.user";
        await tempLogSession.AuthenticateSession(userName);
        var authSession = await getSingleAuthSession(services);
        var startSession = await getSingleStartSession(services);
        Assert.That(authSession.SessionKey, Is.EqualTo(startSession.SessionKey), "Should have the same session key as the start session");
        Assert.That(authSession.UserName, Is.EqualTo(userName), "Should set user name");
    }

    private async Task<AuthenticateSessionModel> getSingleAuthSession(IServiceProvider services)
    {
        var tempLog = getTempLog(services);
        var clock = getClock(services);
        var files = tempLog.AuthSessionFiles(clock.Now()).ToArray();
        Assert.That(files.Length, Is.EqualTo(1), "Should be one auth session file");
        var serializedAuthSession = await files[0].Read();
        return XtiSerializer.Deserialize<AuthenticateSessionModel>(serializedAuthSession);
    }

    [Test]
    public async Task ShouldLogError()
    {
        var services = setup();
        var tempLogSession = getTempLogSession(services);
        await tempLogSession.StartSession();
        var path = "group1/action1";
        await tempLogSession.StartRequest(path);
        Exception thrownException;
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
            thrownException = ex;
        }
        var tempLog = getTempLog(services);
        var clock = getClock(services);
        var requestFiles = tempLog.StartRequestFiles(clock.Now()).ToArray();
        var request = XtiSerializer.Deserialize<StartRequestModel>(await requestFiles[0].Read());
        var logEvent = await getSingleLogEvent(services);
        Assert.That(string.IsNullOrWhiteSpace(logEvent.EventKey), Is.False, "Should create event key");
        Assert.That(logEvent.RequestKey, Is.EqualTo(request.RequestKey), "Should set request key");
        Assert.That(logEvent.Severity, Is.EqualTo(AppEventSeverity.Values.CriticalError.Value), "Should set severity");
        Assert.That(logEvent.Caption, Is.EqualTo("An unexpected error occurred"), "Should set caption");
        Assert.That(logEvent.Message, Is.EqualTo(thrownException.Message), "Should set message");
        Assert.That(logEvent.Detail, Is.EqualTo(thrownException.StackTrace), "Should set detail");
        Assert.That(logEvent.TimeOccurred, Is.EqualTo(clock.Now()), "Should set time occurred to the current time");
    }

    private async Task<LogEventModel> getSingleLogEvent(IServiceProvider services)
    {
        var tempLog = getTempLog(services);
        var files = tempLog.LogEventFiles(DateTime.Now).ToArray();
        Assert.That(files.Length, Is.EqualTo(1), "Should be one log event file");
        var serializedLogEvent = await files[0].Read();
        return XtiSerializer.Deserialize<LogEventModel>(serializedLogEvent);
    }

    private IServiceProvider setup()
    {
        Environment.SetEnvironmentVariable("DOTNET_ENVIRONMENT", "Test");
        var hostBuilder = new XtiHostBuilder();
        hostBuilder.Services.AddMemoryCache();
        hostBuilder.Services.AddFakeTempLogServices();
        hostBuilder.Services.AddSingleton<IClock, FakeClock>();
        hostBuilder.Services.AddScoped<IAppEnvironmentContext>(sp => new FakeAppEnvironmentContext
        {
            Environment = new AppEnvironment
            (
                "test.user", "my-computer", "10.1.0.0", "Windows 10", "WebApp"
            )
        });
        var host = hostBuilder.Build();
        return host.Scope();
    }

    private TempLogSession getTempLogSession(IServiceProvider sp)
        => sp.GetRequiredService<TempLogSession>();

    private FakeTempLog getTempLog(IServiceProvider sp)
        => (FakeTempLog)sp.GetRequiredService<TempLog>();

    private FakeAppEnvironmentContext getAppEnvironmentContext(IServiceProvider sp)
        => (FakeAppEnvironmentContext)sp.GetRequiredService<IAppEnvironmentContext>();

    private FakeClock getClock(IServiceProvider sp) => (FakeClock)sp.GetRequiredService<IClock>();
}