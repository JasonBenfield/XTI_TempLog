using System;
using System.Text.Json;
using XTI_Core;
using XTI_TempLog.Abstractions;

namespace XTI_TempLog;

public sealed class TempLogSession
{
    private readonly TempLogV1 log;
    private readonly TempLogRepository logRepo;
    private readonly IAppEnvironmentContext appEnvironmentContext;
    private readonly IClock clock;
    private readonly CurrentSession currentSession;
    private readonly ThrottledLogs throttledLogs;

    private TempLogSessionModel? session;
    private TempLogRequestModel? request;
    private StartRequestModel? startRequestModel;
    private ThrottledLog? throttledLog;
    private bool isRequestLogged;

    public TempLogSession
    (
        TempLogRepository logRepo,
        TempLogV1 log,
        IAppEnvironmentContext appEnvironmentContext,
        CurrentSession currentSession,
        IClock clock,
        ThrottledLogs throttledLogs
    )
    {
        this.logRepo = logRepo;
        this.log = log;
        this.appEnvironmentContext = appEnvironmentContext;
        this.currentSession = currentSession;
        this.clock = clock;
        this.throttledLogs = throttledLogs;
    }

    public async Task<TempLogSessionModel> StartSession()
    {
        var environment = await appEnvironmentContext.Value();
        session = logRepo.AddOrUpdateSession(currentSession.SessionKey, environment, clock.Now());
        currentSession.SessionKey = session.SessionKey;
        return session;
    }

    public async Task<TempLogSessionModel> AuthenticateSession(string userName)
    {
        var environment = await appEnvironmentContext.Value();
        session = logRepo.AddOrUpdateSession(currentSession.SessionKey, environment, userName, clock.Now());
        currentSession.SessionKey = session.SessionKey;
        return session;
    }

    public Task StartRequest(string path) => StartRequest(path, "");

    public async Task StartRequest(string path, string sourceRequestKey)
    {
        var environment = await appEnvironmentContext.Value();
        throttledLog = throttledLogs.GetThrottledLog(path);
        var requestThrottledLog = throttledLog;
        requestThrottledLog.IncrementRequestCount();
        if (session == null)
        {
            session = logRepo.AddOrUpdateSession(currentSession.SessionKey, environment, clock.Now());
        }
        if (requestThrottledLog.CanLogRequest())
        {
            request = logRepo.AddOrUpdateRequest
            (
                session,
                environment,
                path,
                sourceRequestKey,
                requestThrottledLog.RequestCount,
                clock.Now()
            );
            throttledLog.RequestLogged();
            isRequestLogged = true;
        }
        else
        {
            request = logRepo.CreateRequest
            (
                session,
                environment,
                path,
                sourceRequestKey,
                requestThrottledLog.RequestCount,
                clock.Now()
            );
            isRequestLogged = false;
        }
    }

    private string generateKey() => Guid.NewGuid().ToString("D");

    public string GetCurrentRequestKey() => startRequestModel?.RequestKey ?? "";

    public async Task EndRequest()
    {
        if (isRequestLogged && request != null)
        {
            if(session == null)
            {
                var environment = await appEnvironmentContext.Value();
                session = logRepo.AddOrUpdateSession(currentSession.SessionKey, environment, clock.Now());
            }
            request.TimeEnded = clock.Now();
            logRepo.AddOrUpdateRequest(session, request);
            var serialized = JsonSerializer.Serialize(request);
            await log.Write($"endRequest.{request.RequestKey}.log", serialized);
        }
    }

    public async Task EndSession()
    {
        if (session == null)
        {
            var environment = await appEnvironmentContext.Value();
            session = logRepo.AddOrUpdateSession(currentSession.SessionKey, environment, clock.Now());
        }
        session.TimeEnded = clock.Now();
        logRepo.AddOrUpdateSession(session);
    }

    public async Task LogInformation(string caption, string message, string details = "", string category = "")
    {
        var tempEvent = new LogEntryModel
        {
            EventKey = generateKey(),
            RequestKey = startRequestModel?.RequestKey ?? generateKey(),
            TimeOccurred = clock.Now(),
            Severity = AppEventSeverity.Values.Information.Value,
            Caption = caption,
            Message = message,
            Detail = details,
            Category = category
        };
        if (isRequestLogged)
        {
            await WriteLogEntry
            (
                AppEventSeverity.Values.Information,
                message,
                details,
                caption,
                "",
                category,
                clock.Now(),
                1
            );
        }
    }

    public Task LogException
    (
        AppEventSeverity severity,
        Exception ex,
        string caption,
        string parentEventKey
    ) =>
        LogException(severity, ex, caption, parentEventKey, ex.GetType().Name);

    public Task LogException
    (
        AppEventSeverity severity,
        Exception ex,
        string caption,
        string parentEventKey,
        string category
    ) =>
        LogError(severity, GetExceptionMessage(ex), ex.StackTrace ?? "", caption, parentEventKey, category);

    public async Task LogError
    (
        AppEventSeverity severity,
        string message,
        string detail,
        string caption,
        string parentEventKey,
        string category
    )
    {
        var exceptionThrottledLog = throttledLog;
        if (exceptionThrottledLog == null)
        {
            exceptionThrottledLog = throttledLogs.GetThrottledLog(startRequestModel?.Path ?? "");
        }
        exceptionThrottledLog.IncrementExceptionCount();
        if (exceptionThrottledLog.CanLogException())
        {
            if (!isRequestLogged && request != null)
            {
                if(session == null)
                {
                    var environment = await appEnvironmentContext.Value();
                    session = logRepo.AddOrUpdateSession(currentSession.SessionKey, environment, clock.Now());
                }
                logRepo.AddOrUpdateRequest(session, request);
                isRequestLogged = true;
            }
            await WriteLogEntry
            (
                severity,
                message,
                detail,
                caption,
                parentEventKey,
                category,
                clock.Now(),
                exceptionThrottledLog.ExceptionCount
            );
            exceptionThrottledLog.ExceptionLogged();
        }
    }

    private async Task WriteLogEntry
    (
        AppEventSeverity severity,
        string message,
        string detail,
        string caption,
        string parentEventKey,
        string category,
        DateTimeOffset timeOccurred,
        int actualCount
    )
    {
        if(request != null)
        {
            if (session == null)
            {
                var environment = await appEnvironmentContext.Value();
                session = logRepo.AddOrUpdateSession(currentSession.SessionKey, environment, clock.Now());
            }
            logRepo.AddOrUpdateLogEntry
            (
                session,
                request,
                severity,
                message,
                detail,
                caption,
                parentEventKey,
                category,
                timeOccurred,
                actualCount
            );
        }
    }

    private string GetExceptionMessage(Exception ex)
    {
        var messages = new List<string>();
        var currentEx = ex;
        while (currentEx != null)
        {
            messages.Add(currentEx.Message);
            currentEx = currentEx.InnerException;
        }
        return string.Join("\r\n", messages);
    }
}