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
        var logFiles = await WriteLogFiles(sp);
        Assert.That(logFiles.Length, Is.EqualTo(1));
        var sessionDetails = await logFiles[0].Read();
        Assert.That(sessionDetails.Length, Is.EqualTo(1), "Should log session when starting session");
        Assert.That(sessionDetails[0].Session.SessionKey.ID, Is.Not.EqualTo(""), "Should log session when starting session");
        Assert.That(sessionDetails[0].Session.SessionKey.UserName, Is.EqualTo("test.user"), "Should log session when starting session");
    }

    [Test]
    public async Task ShouldLog_WhenSessionEnds()
    {
        var sp = Setup();
        var tempLogSession = sp.GetRequiredService<TempLogSession>();
        await tempLogSession.StartSession();
        using var scope = sp.CreateScope();
        var requestTempLogSession = scope.ServiceProvider.GetRequiredService<TempLogSession>();
        await requestTempLogSession.StartRequest("/Test/Current");
        await requestTempLogSession.EndRequest();
        await tempLogSession.EndSession();
        var logFiles = await WriteLogFiles(sp);
        var sessionDetails = await GetSessionDetails(logFiles);
        var sessionDetail = sessionDetails.LastOrDefault() ?? new();
        var clock = sp.GetRequiredService<IClock>();
        Assert.That(sessionDetail.Session.TimeEnded, Is.EqualTo(clock.Now()), "Should log when session ends");
    }

    [Test]
    public async Task ShouldLogSession_WhenAuthenticatingSession()
    {
        var sp = Setup();
        var tempLogSession = sp.GetRequiredService<TempLogSession>();
        await tempLogSession.AuthenticateSession("someone.else");
        var logFiles = await WriteLogFiles(sp);
        Assert.That(logFiles.Length, Is.EqualTo(1));
        var sessionDetails = await logFiles[0].Read();
        Assert.That(sessionDetails.Length, Is.EqualTo(1), "Should log session when starting session");
        Assert.That(sessionDetails[0].Session.SessionKey.ID, Is.Not.EqualTo(""), "Should log session when starting session");
        Assert.That(sessionDetails[0].Session.SessionKey.UserName, Is.EqualTo("someone.else"), "Should log session when authenticating session");
    }

    [Test]
    public async Task ShouldReplaceAnonUser_WhenAuthenticatingSession()
    {
        var sp = Setup();
        var tempLogSession = sp.GetRequiredService<TempLogSession>();
        var appEnv = sp.GetRequiredService<FakeAppEnvironmentContext>();
        appEnv.Environment = appEnv.Environment with { UserName = "" };
        var currentSession = sp.GetRequiredService<CurrentSession>();
        currentSession.SessionKey = new SessionKey("Session1", "");
        await tempLogSession.StartRequest("/Test/Current");
        await tempLogSession.AuthenticateSession("someone.else");
        var logFiles = await WriteLogFiles(sp);
        Assert.That(logFiles.Length, Is.EqualTo(1));
        var sessionDetails = await logFiles[0].Read();
        Assert.That(sessionDetails.Length, Is.EqualTo(1), "Should log session when starting session");
        Assert.That(sessionDetails[0].Session.SessionKey.ID, Is.EqualTo("Session1"), "Should log session when authenticating session");
        Assert.That(sessionDetails[0].Session.SessionKey.UserName, Is.EqualTo("someone.else"), "Should log session when authenticating session");
    }

    [Test]
    public async Task ShouldCreateNewSessionKey_WhenUserNameDoesNotMatchSessionKey()
    {
        var sp = Setup();
        var tempLogSession = sp.GetRequiredService<TempLogSession>();
        var appEnv = sp.GetRequiredService<FakeAppEnvironmentContext>();
        appEnv.Environment = appEnv.Environment with { UserName = "someone.else" };
        var currentSession = sp.GetRequiredService<CurrentSession>();
        currentSession.SessionKey = new SessionKey("Session1", "test.user");
        await tempLogSession.StartRequest("/Test/Current");
        await tempLogSession.AuthenticateSession("someone.else");
        var logFiles = await WriteLogFiles(sp);
        Assert.That(logFiles.Length, Is.EqualTo(1));
        var sessionDetails = await logFiles[0].Read();
        Assert.That(sessionDetails.Length, Is.EqualTo(1), "Should log session when starting session");
        Assert.That(sessionDetails[0].Session.SessionKey.ID, Is.Not.EqualTo("Session1"), "Should use new session key when session key user name does not equal user name");
    }

    [Test]
    public async Task ShouldReplaceAnonUser_WhenLoggingRequests()
    {
        var sp = Setup();
        var tempLogSession = sp.GetRequiredService<TempLogSession>();
        var appEnv = sp.GetRequiredService<FakeAppEnvironmentContext>();
        appEnv.Environment = appEnv.Environment with { UserName = "xti_anon" };
        await tempLogSession.StartRequest("/Test/Current1");
        appEnv.Environment = appEnv.Environment with { UserName = "test.user" };
        await tempLogSession.StartRequest("/Test/Current2");
        var logFiles = await WriteLogFiles(sp);
        Assert.That(logFiles.Length, Is.EqualTo(1));
        var sessionDetails = await logFiles[0].Read();
        Assert.That(sessionDetails.Length, Is.EqualTo(1), "Should log session when starting session");
        Assert.That(sessionDetails[0].Session.SessionKey.UserName, Is.EqualTo("test.user"), "Should log session when authenticating session");
    }

    [Test]
    public async Task ShouldLogSession_WhenStartingRequest()
    {
        var sp = Setup();
        var currentSession = sp.GetRequiredService<CurrentSession>();
        currentSession.SessionKey = new SessionKey("Session1", "test.user");
        var tempLogSession = sp.GetRequiredService<TempLogSession>();
        await tempLogSession.StartRequest("/Test/Current");
        var logFiles = await WriteLogFiles(sp);
        Assert.That(logFiles.Length, Is.EqualTo(1));
        var sessionDetails = await logFiles[0].Read();
        Assert.That(sessionDetails.Length, Is.EqualTo(1), "Should log session when starting request");
        Assert.That(sessionDetails[0].Session.SessionKey.ID, Is.EqualTo("Session1"), "Should log session when starting request");
        Assert.That(sessionDetails[0].Session.SessionKey.UserName, Is.EqualTo("test.user"), "Should log session when starting request");
    }

    [Test]
    public async Task ShouldAddRequest_WhenStartingRequest()
    {
        var sp = Setup();
        var currentSession = sp.GetRequiredService<CurrentSession>();
        currentSession.SessionKey = new SessionKey("Session1", "");
        var tempLogSession = sp.GetRequiredService<TempLogSession>();
        await tempLogSession.StartRequest("/Test/Current");
        var logFiles = await WriteLogFiles(sp);
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
        currentSession.SessionKey = new SessionKey("Session1", "");
        var tempLogSession = sp.GetRequiredService<TempLogSession>();
        await tempLogSession.StartRequest("/Test/Current");
        await tempLogSession.EndRequest();
        var logFiles = await WriteLogFiles(sp);
        var sessionDetails = await logFiles[0].Read();
        var request = sessionDetails
            .FirstOrDefault()?.RequestDetails
            .FirstOrDefault()?.Request ??
            new();
        var clock = sp.GetRequiredService<IClock>();
        Assert.That(request.TimeEnded, Is.EqualTo(clock.Now()), "Should log session when starting request");
    }

    [Test]
    public async Task ShouldLogRequestData()
    {
        var sp = Setup();
        var currentSession = sp.GetRequiredService<CurrentSession>();
        currentSession.SessionKey = new SessionKey("Session1", "");
        var tempLogSession = sp.GetRequiredService<TempLogSession>();
        await tempLogSession.StartRequest("/Test/Current");
        await ClearLogFiles(sp);
        var requestData = "Request Data";
        await tempLogSession.LogRequestData(requestData);
        var sessionDetails = await GetSessionDetails(sp);
        var request = sessionDetails
            .FirstOrDefault()?.RequestDetails
            .FirstOrDefault()?.Request ??
            new();
        Assert.That(request.RequestData, Is.EqualTo(requestData), "Should log request data");
    }

    [Test]
    public async Task ShouldLogResultData()
    {
        var sp = Setup();
        var currentSession = sp.GetRequiredService<CurrentSession>();
        currentSession.SessionKey = new SessionKey("Session1", "");
        var tempLogSession = sp.GetRequiredService<TempLogSession>();
        await tempLogSession.StartRequest("/Test/Current");
        await ClearLogFiles(sp);
        var resultData = "Result Data";
        await tempLogSession.LogResultData(resultData);
        var sessionDetails = await GetSessionDetails(sp);
        var request = sessionDetails
            .FirstOrDefault()?.RequestDetails
            .FirstOrDefault()?.Request ??
            new();
        Assert.That(request.ResultData, Is.EqualTo(resultData), "Should log result data");
    }

    [Test]
    public async Task ShouldUpdateTimeEnded_WhenEndingSession()
    {
        var sp = Setup();
        var currentSession = sp.GetRequiredService<CurrentSession>();
        currentSession.SessionKey = new SessionKey("Session1", "");
        var tempLogSession = sp.GetRequiredService<TempLogSession>();
        await tempLogSession.EndSession();
        var logFiles = await WriteLogFiles(sp);
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
        currentSession.SessionKey = new SessionKey("Session1", "");
        var tempLogSession = sp.GetRequiredService<TempLogSession>();
        await tempLogSession.StartRequest("/Test/Current");
        await tempLogSession.LogInformation("Caption", "Message");
        var logFiles = await WriteLogFiles(sp);
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
        currentSession.SessionKey = new SessionKey("Session1", "");
        var tempLogSession = sp.GetRequiredService<TempLogSession>();
        await tempLogSession.StartRequest("/Test/Current");
        await tempLogSession.EndRequest();
        await ClearLogFiles(sp);
        var logFiles = await WriteLogFiles(sp);
        Assert.That(logFiles.Length, Is.EqualTo(0));
    }

    [Test]
    public async Task ShouldLogRequest_WhenRequestIsStartedAfterWritingLog()
    {
        var sp = Setup();
        var currentSession = sp.GetRequiredService<CurrentSession>();
        var tempLogSession = sp.GetRequiredService<TempLogSession>();
        await tempLogSession.StartSession();
        await ClearLogFiles(sp);
        await tempLogSession.StartRequest("/Test/Current");
        var logFiles = await WriteLogFiles(sp);
        var sessionDetails = await logFiles[0].Read();
        var session = sessionDetails.FirstOrDefault()?.Session ?? new();
        Assert.That(session.SessionKey.ID, Is.Not.EqualTo(""));
        Assert.That(session.SessionKey.UserName, Is.EqualTo("test.user"));
        var clock = sp.GetRequiredService<IClock>();
        Assert.That(session.TimeStarted, Is.EqualTo(clock.Now()));
        var request = sessionDetails
            .FirstOrDefault()?.RequestDetails
            .FirstOrDefault()?
            .Request ?? new();
        Assert.That(request.RequestKey, Is.Not.EqualTo(""));
        Assert.That(request.Path, Is.EqualTo("/Test/Current"));
        Assert.That(request.TimeStarted, Is.EqualTo(clock.Now()));
    }

    [Test]
    public async Task ShouldEndRequest_WhenRequestIsEndedAfterWritingLog()
    {
        var sp = Setup();
        var currentSession = sp.GetRequiredService<CurrentSession>();
        var tempLogSession = sp.GetRequiredService<TempLogSession>();
        await tempLogSession.StartRequest("/Test/Current");
        await ClearLogFiles(sp);
        await tempLogSession.EndRequest();
        var logFiles = await WriteLogFiles(sp);
        var sessionDetails = await logFiles[0].Read();
        var request = sessionDetails
            .FirstOrDefault()?.RequestDetails
            .FirstOrDefault()?
            .Request ?? new();
        Assert.That(request.RequestKey, Is.Not.EqualTo(""));
        Assert.That(request.Path, Is.EqualTo("/Test/Current"));
        var clock = sp.GetRequiredService<IClock>();
        Assert.That(request.TimeStarted, Is.EqualTo(clock.Now()));
        Assert.That(request.TimeEnded, Is.EqualTo(clock.Now()));
    }

    [Test]
    public async Task ShouldAddLogEntry_WhenLoggingInformationAfterWritingLog()
    {
        var sp = Setup();
        var currentSession = sp.GetRequiredService<CurrentSession>();
        var tempLogSession = sp.GetRequiredService<TempLogSession>();
        await tempLogSession.StartRequest("/Test/Current");
        var logFiles = await WriteLogFiles(sp);
        await tempLogSession.LogInformation("Caption", "Message");
        DeleteLogFiles(logFiles);
        logFiles = await WriteLogFiles(sp);
        var sessionDetails = await logFiles[0].Read();
        var session = sessionDetails.FirstOrDefault()?.Session ?? new();
        Assert.That(session.SessionKey.ID, Is.Not.EqualTo(""));
        Assert.That(session.SessionKey.UserName, Is.EqualTo("test.user"));
        var clock = sp.GetRequiredService<IClock>();
        Assert.That(session.TimeStarted, Is.EqualTo(clock.Now()));
        var request = sessionDetails
            .FirstOrDefault()?.RequestDetails
            .FirstOrDefault()?
            .Request ?? new();
        Assert.That(request.RequestKey, Is.Not.EqualTo(""));
        Assert.That(request.Path, Is.EqualTo("/Test/Current"));
        Assert.That(request.TimeStarted, Is.EqualTo(clock.Now()));
        var logEntry = sessionDetails
            .FirstOrDefault()?.RequestDetails?
            .FirstOrDefault()?.LogEntries
            .FirstOrDefault() ??
            new();
        Assert.That(logEntry.Caption, Is.EqualTo("Caption"), "Should add log entry when logging information");
        Assert.That(logEntry.TimeOccurred, Is.EqualTo(clock.Now()), "Should add log entry when logging information");
    }

    private IServiceProvider Setup()
    {
        Environment.SetEnvironmentVariable("DOTNET_ENVIRONMENT", "Test");
        var hostBuilder = new XtiHostBuilder();
        hostBuilder.Services.AddMemoryCache();
        hostBuilder.Services.AddFakeTempLogServices();
        hostBuilder.Services.AddSingleton<IClock, FakeClock>();
        hostBuilder.Services.AddSingleton
        (
            sp => new FakeAppEnvironmentContext
            {
                Environment = new AppEnvironment
                (
                    "test.user", "my-computer", "10.1.0.0", "Windows 10", 123
                )
            }
        );
        hostBuilder.Services.AddSingleton<IAppEnvironmentContext>(sp => sp.GetRequiredService<FakeAppEnvironmentContext>());
        var host = hostBuilder.Build();
        return host.Scope();
    }

    private static async Task<ITempLogFile[]> ClearLogFiles(IServiceProvider sp)
    {
        var logFiles = await WriteLogFiles(sp);
        DeleteLogFiles(logFiles);
        return logFiles;
    }

    private static async Task<ITempLogFile[]> WriteLogFiles(IServiceProvider sp)
    {
        var tempLogRepo = sp.GetRequiredService<TempLogRepository>();
        await tempLogRepo.WriteToLocalStorage();
        var tempLog = sp.GetRequiredService<TempLog>();
        var logFiles = tempLog.Files(DateTime.Now, 100);
        return logFiles;
    }

    private static void DeleteLogFiles(ITempLogFile[] logFiles)
    {
        foreach (var logFile in logFiles)
        {
            logFile.Delete();
        }
    }

    private static async Task<TempLogSessionDetailModel[]> GetSessionDetails(IServiceProvider sp)
    {
        var logFiles = await WriteLogFiles(sp);
        var sessionDetails = await GetSessionDetails(logFiles);
        return sessionDetails;
    }

    private static async Task<TempLogSessionDetailModel[]> GetSessionDetails(ITempLogFile[] logFiles)
    {
        var sessionDetails = new List<TempLogSessionDetailModel>();
        foreach (var logFile in logFiles)
        {
            var fileSessionDetails = await logFile.Read();
            sessionDetails.AddRange(fileSessionDetails);
        }
        return sessionDetails.ToArray();
    }

}
