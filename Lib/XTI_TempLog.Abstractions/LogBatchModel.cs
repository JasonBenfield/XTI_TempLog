namespace XTI_TempLog.Abstractions;

public sealed class LogBatchModel
{
    private StartSessionModel[] startSessions = new StartSessionModel[0];
    private StartRequestModel[] startRequests = new StartRequestModel[0];
    private LogEntryModelV1[] logEntries = new LogEntryModelV1[0];
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

    public LogEntryModelV1[] LogEntries
    {
        get => logEntries;
        set => logEntries = value ?? new LogEntryModelV1[0];
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
}