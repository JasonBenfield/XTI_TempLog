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
    public async Task ShouldLogSession_WhenStartingSession()
    {
        var sp = Setup();
        var tempLogSession = sp.GetRequiredService<TempLogSession>();
        await tempLogSession.StartSession();
        var tempLogRepo = sp.GetRequiredService<TempLogRepository>();
        await tempLogRepo.WriteToLocalStorage();
        var tempLog = sp.GetRequiredService<TempLog>();
        var logFiles = tempLog.Files(DateTime.Now, 100);
        Assert.That(logFiles.Length, Is.EqualTo(1));
        var sessionDetails = await logFiles[0].Read();
        Assert.That(sessionDetails.Length, Is.EqualTo(1), "Should log session when starting session");
        Assert.That(sessionDetails[0].Session.SessionKey, Is.Not.EqualTo(""), "Should log session when starting session");
        Assert.That(sessionDetails[0].Session.UserName, Is.EqualTo("test.user"), "Should log session when starting session");
    }

    [Test]
    public async Task ShouldLogSession_WhenAuthenticatingSession()
    {
        var sp = Setup();
        var tempLogSession = sp.GetRequiredService<TempLogSession>();
        await tempLogSession.AuthenticateSession("someone.else");
        var tempLogRepo = sp.GetRequiredService<TempLogRepository>();
        await tempLogRepo.WriteToLocalStorage();
        var tempLog = sp.GetRequiredService<TempLog>();
        var logFiles = tempLog.Files(DateTime.Now, 100);
        Assert.That(logFiles.Length, Is.EqualTo(1));
        var sessionDetails = await logFiles[0].Read();
        Assert.That(sessionDetails.Length, Is.EqualTo(1), "Should log session when starting session");
        Assert.That(sessionDetails[0].Session.SessionKey, Is.Not.EqualTo(""), "Should log session when starting session");
        Assert.That(sessionDetails[0].Session.UserName, Is.EqualTo("someone.else"), "Should log session when authenticating session");
    }

    [Test]
    public async Task ShouldLogSession_WhenStartingRequest()
    {
        var sp = Setup();
        var currentSession = sp.GetRequiredService<CurrentSession>();
        currentSession.SessionKey = "Session1";
        var tempLogSession = sp.GetRequiredService<TempLogSession>();
        await tempLogSession.StartRequest("/Test/Current");
        var tempLogRepo = sp.GetRequiredService<TempLogRepository>();
        await tempLogRepo.WriteToLocalStorage();
        var tempLog = sp.GetRequiredService<TempLog>();
        var logFiles = tempLog.Files(DateTime.Now, 100);
        Assert.That(logFiles.Length, Is.EqualTo(1));
        var sessionDetails = await logFiles[0].Read();
        Assert.That(sessionDetails.Length, Is.EqualTo(1), "Should log session when starting request");
        Assert.That(sessionDetails[0].Session.SessionKey, Is.EqualTo("Session1"), "Should log session when starting request");
        Assert.That(sessionDetails[0].Session.UserName, Is.EqualTo("test.user"), "Should log session when starting request");
    }

    [Test]
    public async Task ShouldAddRequest_WhenStartingRequest()
    {
        var sp = Setup();
        var currentSession = sp.GetRequiredService<CurrentSession>();
        currentSession.SessionKey = "Session1";
        var tempLogSession = sp.GetRequiredService<TempLogSession>();
        await tempLogSession.StartRequest("/Test/Current");
        var tempLogRepo = sp.GetRequiredService<TempLogRepository>();
        await tempLogRepo.WriteToLocalStorage();
        var tempLog = sp.GetRequiredService<TempLog>();
        var logFiles = tempLog.Files(DateTime.Now, 100);
        var sessionDetails = await logFiles[0].Read();
        var request = sessionDetails
            .FirstOrDefault()?.RequestDetails
            .FirstOrDefault()?.Request ??
            new();
        var clock = sp.GetRequiredService<IClock>();
        Assert.That(request.RequestKey, Is.Not.EqualTo(""), "Should add request when starting request");
        Assert.That(request.Path, Is.EqualTo("/Test/Current"), "Should add request when starting request");
        Assert.That(request.TimeStarted, Is.EqualTo(clock.Now()), "Should add request when starting request");
    }

    [Test]
    public async Task ShouldUpdateTimeEnded_WhenEndingRequest()
    {
        var sp = Setup();
        var currentSession = sp.GetRequiredService<CurrentSession>();
        currentSession.SessionKey = "Session1";
        var tempLogSession = sp.GetRequiredService<TempLogSession>();
        await tempLogSession.StartRequest("/Test/Current");
        await tempLogSession.EndRequest();
        var tempLogRepo = sp.GetRequiredService<TempLogRepository>();
        await tempLogRepo.WriteToLocalStorage();
        var tempLog = sp.GetRequiredService<TempLog>();
        var logFiles = tempLog.Files(DateTime.Now, 100);
        var sessionDetails = await logFiles[0].Read();
        var request = sessionDetails
            .FirstOrDefault()?.RequestDetails
            .FirstOrDefault()?.Request ?? 
            new();
        var clock = sp.GetRequiredService<IClock>();
        Assert.That(request.TimeEnded, Is.EqualTo(clock.Now()), "Should log session when starting request");
    }

    [Test]
    public async Task ShouldUpdateTimeEnded_WhenEndingSession()
    {
        var sp = Setup();
        var currentSession = sp.GetRequiredService<CurrentSession>();
        currentSession.SessionKey = "Session1";
        var tempLogSession = sp.GetRequiredService<TempLogSession>();
        await tempLogSession.EndSession();
        var tempLogRepo = sp.GetRequiredService<TempLogRepository>();
        await tempLogRepo.WriteToLocalStorage();
        var tempLog = sp.GetRequiredService<TempLog>();
        var logFiles = tempLog.Files(DateTime.Now, 100);
        var sessionDetails = await logFiles[0].Read();
        var session = sessionDetails
            .FirstOrDefault()?.Session ??
            new();
        var clock = sp.GetRequiredService<IClock>();
        Assert.That(session.TimeEnded, Is.EqualTo(clock.Now()), "Should update time ended when ending session");
    }

    [Test]
    public async Task ShouldAddLogEntry_WhenLoggingInformation()
    {
        var sp = Setup();
        var currentSession = sp.GetRequiredService<CurrentSession>();
        currentSession.SessionKey = "Session1";
        var tempLogSession = sp.GetRequiredService<TempLogSession>();
        await tempLogSession.StartRequest("/Test/Current");
        await tempLogSession.LogInformation("Caption", "Message");
        var tempLogRepo = sp.GetRequiredService<TempLogRepository>();
        await tempLogRepo.WriteToLocalStorage();
        var tempLog = sp.GetRequiredService<TempLog>();
        var logFiles = tempLog.Files(DateTime.Now, 100);
        var sessionDetails = await logFiles[0].Read();
        var logEntry = sessionDetails
            .FirstOrDefault()?.RequestDetails?
            .FirstOrDefault()?.LogEntries
            .FirstOrDefault() ??
            new();
        Assert.That(logEntry.Caption, Is.EqualTo("Caption"), "Should add log entry when logging information");
        var clock = sp.GetRequiredService<IClock>();
        Assert.That(logEntry.TimeOccurred, Is.EqualTo(clock.Now()), "Should add log entry when logging information");
    }

    [Test]
    public async Task ShouldClearSessions_AfterWritingToLocalStorage()
    {
        var sp = Setup();
        var currentSession = sp.GetRequiredService<CurrentSession>();
        currentSession.SessionKey = "Session1";
        var tempLogSession = sp.GetRequiredService<TempLogSession>();
        await tempLogSession.StartRequest("/Test/Current");
        await tempLogSession.EndRequest();
        var tempLogRepo = sp.GetRequiredService<TempLogRepository>();
        await tempLogRepo.WriteToLocalStorage();
        var tempLog = sp.GetRequiredService<TempLog>();
        var logFiles = tempLog.Files(DateTime.Now, 100);
        foreach(var logFile in logFiles)
        {
            logFile.Delete();
        }
        await tempLogRepo.WriteToLocalStorage();
        logFiles = tempLog.Files(DateTime.Now, 100);
        Assert.That(logFiles.Length, Is.EqualTo(0));
    }

    private IServiceProvider Setup()
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
                "test.user", "my-computer", "10.1.0.0", "Windows 10", 123
            )
        });
        var host = hostBuilder.Build();
        return host.Scope();
    }

}
