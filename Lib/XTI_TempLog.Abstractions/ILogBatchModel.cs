namespace XTI_TempLog.Abstractions
{
    public interface ILogBatchModel
    {
        IAuthenticateSessionModel[] AuthenticateSessions { get; set; }
        IEndRequestModel[] EndRequests { get; set; }
        ILogEventModel[] LogEvents { get; set; }
        IStartRequestModel[] StartRequests { get; set; }
        IStartSessionModel[] StartSessions { get; set; }
        IEndSessionModel[] EndSessions { get; set; }
    }
}