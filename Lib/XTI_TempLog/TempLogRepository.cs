﻿using System.Collections.Concurrent;
using XTI_Core;
using XTI_TempLog.Abstractions;

namespace XTI_TempLog;

public sealed class TempLogRepository
{
    private readonly ConcurrentDictionary<string, TempLogSessionModel> sessionDict = new();
    private readonly ConcurrentDictionary<string, FlattenedLogEntryModel> flattenedLogEntryDict = new();
    private readonly string affix;
    private readonly TempLog tempLog;
    private readonly string instanceID;
    private int keyID = 1;

    public TempLogRepository(string affix, TempLog tempLog)
    {
        this.affix = affix;
        this.tempLog = tempLog;
        instanceID = Guid.NewGuid().ToString("N");
    }

    internal TempLogSessionModel AddOrUpdateSession
    (
        string sessionKey,
        AppEnvironment environment,
        DateTimeOffset timeStarted
    ) =>
        AddOrUpdateSession(sessionKey, environment, environment.UserName, timeStarted);

    internal TempLogSessionModel AddOrUpdateSession
    (
        string sessionKey,
        AppEnvironment environment,
        string userName,
        DateTimeOffset timeStarted
    )
    {
        if (string.IsNullOrWhiteSpace(sessionKey))
        {
            sessionKey = GenerateKey("ses");
        }
        var session = new TempLogSessionModel
        {
            SessionKey = sessionKey,
            TimeStarted = timeStarted,
            UserName = userName,
            UserAgent = environment.UserAgent,
            RemoteAddress = environment.RemoteAddress,
            RequesterKey = environment.RequesterKey
        };
        session = sessionDict.AddOrUpdate(session.SessionKey, session, (sk, s) => s);
        return session;
    }

    internal TempLogSessionModel AddOrUpdateSession(TempLogSessionModel session) =>
        session = sessionDict.AddOrUpdate(session.SessionKey, session, (sk, s) => s);

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
        session = sessionDict.AddOrUpdate(session.SessionKey, session, (sk, s) => s);
        var request = CreateRequest(session, environment, path, sourceRequestKey, actualCount, timeStarted);
        var flattenedLogEntry = new FlattenedLogEntryModel
        {
            Session = session,
            Request = request
        };
        flattenedLogEntryDict.AddOrUpdate(request.RequestKey, flattenedLogEntry, (k, le) => le);
        return request;
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
            SessionKey = session.SessionKey,
            SourceRequestKey = sourceRequestKey,
            InstallationID = environment.InstallationID,
            Path = path,
            TimeStarted = timeStarted,
            ActualCount = actualCount
        };
    }

    internal void AddOrUpdateRequest(TempLogSessionModel session, TempLogRequestModel request)
    {
        session = sessionDict.AddOrUpdate(session.SessionKey, session, (sk, s) => s);
        flattenedLogEntryDict.GetOrAdd
        (
            request.RequestKey,
            r => new FlattenedLogEntryModel { Session = session, Request = request }
        );
    }

    internal void AddOrUpdateLogEntry
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
            RequestKey = request.RequestKey,
            TimeOccurred = timeOccurred,
            Severity = severity.Value,
            Caption = caption,
            Message = message,
            Detail = detail,
            ParentEventKey = parentEventKey,
            Category = category,
            ActualCount = actualCount
        };
        session = sessionDict.AddOrUpdate(session.SessionKey, session, (sk, s) => s);
        flattenedLogEntryDict.TryAdd
        (
            $"{request.RequestKey}{logEntry.EventKey}",
            new FlattenedLogEntryModel { Session = session, Request = request, LogEntry = logEntry }
        );
    }

    private string GenerateKey(string keyType)
    {
        var key = $"{keyType}{instanceID}{keyID:000000}";
        keyID++;
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
                if (!sessions.Contains(flattenedLogEntry.Session))
                {
                    sessions.Add(flattenedLogEntry.Session);
                }
            }
        }
        var sessionDetails = new List<TempLogSessionDetailModel>();
        foreach (var session in sessions)
        {
            var sessionLogEntries = flattenedLogEntries
                .Where(sle => sle.Session == session);
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
            await tempLog.Write($"{DateTime.Now:yyMMddHHmmssfffffff}_{affix}.log", sessionDetails.ToArray());
        }
    }
}