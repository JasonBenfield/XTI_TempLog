using System.Linq;
using XTI_TempLog.Abstractions;

namespace XTI_TempLog
{
    public sealed class LogBatchModel : ILogBatchModel
    {
        public StartSessionModel[] StartSessions { get; set; } = new StartSessionModel[] { };
        public StartRequestModel[] StartRequests { get; set; } = new StartRequestModel[] { };
        public LogEventModel[] LogEvents { get; set; } = new LogEventModel[] { };
        public EndRequestModel[] EndRequests { get; set; } = new EndRequestModel[] { };
        public AuthenticateSessionModel[] AuthenticateSessions { get; set; } = new AuthenticateSessionModel[] { };
        public EndSessionModel[] EndSessions { get; set; } = new EndSessionModel[] { };

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
}
