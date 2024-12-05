using XTI_Core;
using XTI_TempLog.Abstractions;

namespace XTI_TempLog;

public sealed class TempLogSession
{
    private readonly TempLogRepository logRepo;
    private readonly IAppEnvironmentContext appEnvironmentContext;
    private readonly IClock clock;
    private readonly CurrentSession currentSession;
    private readonly ThrottledLogs throttledLogs;

    private TempLogSessionModel? session;
    private TempLogRequestModel? request;
    private ThrottledLog? throttledLog;
    private bool isRequestLogged;

    public TempLogSession
    (
        TempLogRepository logRepo,
        IAppEnvironmentContext appEnvironmentContext,
        CurrentSession currentSession,
        IClock clock,
        ThrottledLogs throttledLogs
    )
    {
        this.logRepo = logRepo;
        this.appEnvironmentContext = appEnvironmentContext;
        this.currentSession = currentSession;
        this.clock = clock;
        this.throttledLogs = throttledLogs;
    }

    public string GetCurrentRequestKey() => request?.RequestKey ?? "";

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
            currentSession.SessionKey = session.SessionKey;
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

    public async Task EndRequest()
    {
        if (isRequestLogged && request != null)
        {
            if (session == null)
            {
                var environment = await appEnvironmentContext.Value();
                session = logRepo.AddOrUpdateSession(currentSession.SessionKey, environment, clock.Now());
            }
            request.TimeEnded = clock.Now();
            logRepo.AddOrUpdateRequest(session, request);
        }
    }

    public async Task LogRequestData(string requestData)
    {
        if (isRequestLogged && request != null)
        {
            if (session == null)
            {
                var environment = await appEnvironmentContext.Value();
                session = logRepo.AddOrUpdateSession(currentSession.SessionKey, environment, clock.Now());
            }
            request.RequestData = requestData;
            logRepo.AddOrUpdateRequest(session, request);
        }
    }

    public async Task LogResultData(string resultData)
    {
        if (isRequestLogged && request != null)
        {
            if (session == null)
            {
                var environment = await appEnvironmentContext.Value();
                session = logRepo.AddOrUpdateSession(currentSession.SessionKey, environment, clock.Now());
            }
            request.ResultData = resultData;
            logRepo.AddOrUpdateRequest(session, request);
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
        logRepo.EndSession(session);
    }

    public async Task<LogEntryModel> LogInformation(string caption, string message, string details = "", string category = "")
    {
        LogEntryModel logEntry;
        if (isRequestLogged)
        {
            logEntry = await WriteLogEntry
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
        else
        {
            logEntry = new();
        }
        return logEntry;
    }

    public Task<LogEntryModel> LogException
    (
        AppEventSeverity severity,
        Exception ex,
        string caption,
        string parentEventKey
    ) =>
        LogException(severity, ex, caption, parentEventKey, ex.GetType().Name);

    public Task<LogEntryModel> LogException
    (
        AppEventSeverity severity,
        Exception ex,
        string caption,
        string parentEventKey,
        string category
    ) =>
        LogError(severity, GetExceptionMessage(ex), ex.StackTrace ?? "", caption, parentEventKey, category);

    public async Task<LogEntryModel> LogError
    (
        AppEventSeverity severity,
        string message,
        string detail,
        string caption,
        string parentEventKey,
        string category
    )
    {
        LogEntryModel logEntry;
        var exceptionThrottledLog = throttledLog;
        if (exceptionThrottledLog == null)
        {
            exceptionThrottledLog = throttledLogs.GetThrottledLog(request?.Path ?? "");
        }
        exceptionThrottledLog.IncrementExceptionCount();
        if (exceptionThrottledLog.CanLogException())
        {
            if (!isRequestLogged && request != null)
            {
                if (session == null)
                {
                    var environment = await appEnvironmentContext.Value();
                    session = logRepo.AddOrUpdateSession(currentSession.SessionKey, environment, clock.Now());
                }
                logRepo.AddOrUpdateRequest(session, request);
                isRequestLogged = true;
            }
            logEntry = await WriteLogEntry
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
        else
        {
            logEntry = new();
        }
        return logEntry;
    }

    private async Task<LogEntryModel> WriteLogEntry
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
        LogEntryModel logEntry;
        if (request == null)
        {
            logEntry = new();
        }
        else
        {
            if (session == null)
            {
                var environment = await appEnvironmentContext.Value();
                session = logRepo.AddOrUpdateSession(currentSession.SessionKey, environment, clock.Now());
            }
            logEntry = logRepo.AddOrUpdateLogEntry
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
        return logEntry;
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