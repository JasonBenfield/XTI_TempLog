using XTI_TempLog.Abstractions;

namespace XTI_TempLog
{
    public sealed class LogBatchModel : ILogBatchModel
    {
        public IStartSessionModel[] StartSessions { get; set; } = new IStartSessionModel[] { };
        public IStartRequestModel[] StartRequests { get; set; } = new IStartRequestModel[] { };
        public ILogEventModel[] LogEvents { get; set; } = new ILogEventModel[] { };
        public IEndRequestModel[] EndRequests { get; set; } = new IEndRequestModel[] { };
        public IAuthenticateSessionModel[] AuthenticateSessions { get; set; } = new IAuthenticateSessionModel[] { };
    }
}
