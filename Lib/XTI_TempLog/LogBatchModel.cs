using XTI_TempLog.Abstractions;

namespace XTI_TempLog;

public sealed class LogBatchModel : ILogBatchModel
{
    private StartSessionModel[] startSessions = new StartSessionModel[0];
    private StartRequestModel[] startRequests = new StartRequestModel[0];
    private LogEventModel[] logEvents = new LogEventModel[0];
    private EndRequestModel[] endRequests = new EndRequestModel[0];
    private AuthenticateSessionModel[] authenticateSessions = new AuthenticateSessionModel[0];
    private EndSessionModel[] endSessions = new EndSessionModel[0];

    public StartSessionModel[] StartSessions
    {
        get => startSessions;
        set => startSessions = value ?? new StartSessionModel[0];
    }

    public StartRequestModel[] StartRequests
    {
        get => startRequests;
        set => startRequests = value ?? new StartRequestModel[0];
    }

    public LogEventModel[] LogEvents
    {
        get => logEvents;
        set => logEvents = value ?? new LogEventModel[0];
    }

    public EndRequestModel[] EndRequests
    {
        get => endRequests;
        set => endRequests = value ?? new EndRequestModel[0];
    }

    public AuthenticateSessionModel[] AuthenticateSessions
    {
        get => authenticateSessions;
        set => authenticateSessions = value ?? new AuthenticateSessionModel[0];
    }

    public EndSessionModel[] EndSessions
    {
        get => endSessions;
        set => endSessions = value ?? new EndSessionModel[0];
    }

    IAuthenticateSessionModel[] ILogBatchModel.AuthenticateSessions
    {
        get => AuthenticateSessions;
        set => AuthenticateSessions = (value ?? new AuthenticateSessionModel[] { }).Cast<AuthenticateSessionModel>().ToArray();
    }
    IEndRequestModel[] ILogBatchModel.EndRequests
    {
        get => EndRequests;
        set => EndRequests = (value ?? new EndRequestModel[] { }).Cast<EndRequestModel>().ToArray();
    }
    ILogEventModel[] ILogBatchModel.LogEvents
    {
        get => LogEvents;
        set => LogEvents = (value ?? new LogEventModel[] { }).Cast<LogEventModel>().ToArray();
    }
    IStartRequestModel[] ILogBatchModel.StartRequests
    {
        get => StartRequests;
        set => StartRequests = (value ?? new StartRequestModel[] { }).Cast<StartRequestModel>().ToArray();
    }
    IStartSessionModel[] ILogBatchModel.StartSessions
    {
        get => StartSessions;
        set => StartSessions = (value ?? new StartSessionModel[] { }).Cast<StartSessionModel>().ToArray();
    }
    IEndSessionModel[] ILogBatchModel.EndSessions
    {
        get => EndSessions;
        set => EndSessions = (value ?? new EndSessionModel[] { }).Cast<EndSessionModel>().ToArray();
    }
}