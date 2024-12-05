using System.Collections.Concurrent;
using XTI_Core;
using XTI_TempLog.Abstractions;

namespace XTI_TempLog;

public sealed class TempLogRepository
{
    private readonly ConcurrentDictionary<string, TempLogSessionModel> sessionDict = new();
    private readonly ConcurrentDictionary<string, FlattenedLogEntryModel> flattenedLogEntryDict = new();
    private readonly string logSource;
    private readonly TempLog tempLog;
    private readonly string instanceID;
    private static readonly Lock counterLock = new();
    private int keyID = 1;

    public TempLogRepository(TempLog tempLog)
        : this(tempLog, Guid.NewGuid().ToString("N"))
    {
    }

    public TempLogRepository(TempLog tempLog, string logSource)
    {
        this.logSource = logSource;
        this.tempLog = tempLog;
        instanceID = Guid.NewGuid().ToString("N");
    }

    internal TempLogSessionModel AddOrUpdateSession
    (
        SessionKey sessionKey,
        AppEnvironment environment,
        DateTimeOffset timeStarted
    ) =>
        AddOrUpdateSession(sessionKey, environment, environment.UserName, timeStarted);

    internal TempLogSessionModel AddOrUpdateSession
    (
        SessionKey sessionKey,
        AppEnvironment environment,
        string userName,
        DateTimeOffset timeStarted
    )
    {
        if (!sessionKey.IsEmpty() && sessionKey.IsUserNameBlank())
        {
            sessionKey = sessionKey with { UserName = userName };
        }
        else if 
        (
            sessionKey.IsEmpty() || 
            (!string.IsNullOrWhiteSpace(userName) && !sessionKey.HasUserName(userName))
        )
        {
            sessionKey = new SessionKey(GenerateKey("ses"), userName);
        }
        var session = new TempLogSessionModel
        {
            SessionKey = sessionKey,
            TimeStarted = timeStarted,
            UserAgent = environment.UserAgent,
            RemoteAddress = environment.RemoteAddress,
            RequesterKey = environment.RequesterKey
        };
        session = RefreshSession(session);
        return session;
    }

    internal TempLogSessionModel EndSession(TempLogSessionModel session) =>
        session = sessionDict.AddOrUpdate(session.SessionKey.ID, session, (sk, s) => session);

    internal TempLogRequestModel AddOrUpdateRequest
    (
        TempLogSessionModel session,
        AppEnvironment environment,
        string path,
        string sourceRequestKey,
        int actualCount,
        DateTimeOffset timeStarted
    )
    {
        if (!session.SessionKey.HasUserName(environment.UserName) && !string.IsNullOrWhiteSpace(environment.UserName))
        {
            session.SessionKey = session.SessionKey with { UserName = environment.UserName };
            session = RefreshSession(session);
        }
        var request = CreateRequest(session, environment, path, sourceRequestKey, actualCount, timeStarted);
        var flattenedLogEntry = new FlattenedLogEntryModel
        {
            Session = session,
            Request = request
        };
        flattenedLogEntryDict.AddOrUpdate(request.RequestKey, flattenedLogEntry, (k, le) => flattenedLogEntry);
        return request;
    }

    private TempLogSessionModel RefreshSession(TempLogSessionModel session)
    {
        session = sessionDict.AddOrUpdate(session.SessionKey.ID, session, (sk, s) => session);
        foreach (var flattenedLogEntry in flattenedLogEntryDict.Values)
        {
            if (flattenedLogEntry.Session.SessionKey.ID == session.SessionKey.ID)
            {
                flattenedLogEntry.Session = session;
            }
        }
        return session;
    }

    internal TempLogRequestModel CreateRequest
    (
        TempLogSessionModel session,
        AppEnvironment environment,
        string path,
        string sourceRequestKey,
        int actualCount,
        DateTimeOffset timeStarted
    )
    {
        return new TempLogRequestModel
        {
            RequestKey = GenerateKey("req"),
            SourceRequestKey = sourceRequestKey,
            InstallationID = environment.InstallationID,
            Path = path,
            TimeStarted = timeStarted,
            ActualCount = actualCount
        };
    }

    internal void AddOrUpdateRequest(TempLogSessionModel session, TempLogRequestModel request)
    {
        session = sessionDict.AddOrUpdate(session.SessionKey.ID, session, (sk, s) => s);
        flattenedLogEntryDict.GetOrAdd
        (
            request.RequestKey,
            r => new FlattenedLogEntryModel { Session = session, Request = request }
        );
    }

    internal LogEntryModel AddOrUpdateLogEntry
    (
        TempLogSessionModel session,
        TempLogRequestModel request,
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
        var logEntry = new LogEntryModel
        {
            EventKey = GenerateKey("evt"),
            TimeOccurred = timeOccurred,
            Severity = severity.Value,
            Caption = caption,
            Message = message,
            Detail = detail,
            ParentEventKey = parentEventKey,
            Category = category,
            ActualCount = actualCount
        };
        session = sessionDict.AddOrUpdate(session.SessionKey.ID, session, (sk, s) => s);
        flattenedLogEntryDict.TryAdd
        (
            $"{request.RequestKey}{logEntry.EventKey}",
            new FlattenedLogEntryModel { Session = session, Request = request, LogEntry = logEntry }
        );
        return logEntry;
    }

    private string GenerateKey(string keyType)
    {
        string key;
        lock (counterLock)
        {
            key = $"{keyType}{instanceID}{keyID:000000}";
            keyID++;
        }
        return key;
    }

    public async Task WriteToLocalStorage()
    {
        var sessions = new List<TempLogSessionModel>();
        foreach (var key in sessionDict.Keys)
        {
            if (sessionDict.TryRemove(key, out var session))
            {
                sessions.Add(session);
            }
        }
        var flattenedLogEntries = new List<FlattenedLogEntryModel>();
        foreach (var key in flattenedLogEntryDict.Keys)
        {
            if (flattenedLogEntryDict.TryRemove(key, out var flattenedLogEntry))
            {
                flattenedLogEntries.Add(flattenedLogEntry);
                if (!sessions.Any(s => s.SessionKey == flattenedLogEntry.Session.SessionKey))
                {
                    sessions.Add(flattenedLogEntry.Session);
                }
            }
        }
        var sessionDetails = new List<TempLogSessionDetailModel>();
        foreach (var session in sessions)
        {
            var sessionLogEntries = flattenedLogEntries
                .Where(sle => sle.Session.SessionKey == session.SessionKey);
            var requests = sessionLogEntries
                .Select(sle => sle.Request)
                .Where(sle => !string.IsNullOrWhiteSpace(sle.RequestKey))
                .Distinct();
            var requestDetails = new List<TempLogRequestDetailModel>();
            var logEntries = new List<LogEntryModel>();
            foreach (var request in requests)
            {
                var requestDetail = new TempLogRequestDetailModel
                {
                    Request = request,
                    LogEntries = flattenedLogEntries
                        .Where(sle => sle.Request == request && !string.IsNullOrWhiteSpace(sle.LogEntry.EventKey))
                        .Select(sle => sle.LogEntry)
                        .ToArray()
                };
                requestDetails.Add(requestDetail);
            }
            var sessionDetail = new TempLogSessionDetailModel
            {
                Session = session,
                RequestDetails = requestDetails.ToArray()
            };
            sessionDetails.Add(sessionDetail);
        }
        if (sessionDetails.Any())
        {
            await tempLog.Write($"{logSource}_{DateTime.Now:yyMMddHHmmssfffffff}.log", sessionDetails.ToArray());
        }
    }

    public async Task AutoWriteToLocalStorage(TimeSpan interval, CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested)
            {
                await Task.Delay(interval, ct);
                try
                {
                    await WriteToLocalStorage();
                }
                catch { }
            }
        }
        catch (OperationCanceledException)
        {
        }
        try
        {
            await WriteToLocalStorage();
        }
        catch { }
    }
}
