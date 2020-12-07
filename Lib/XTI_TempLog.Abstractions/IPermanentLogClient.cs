using System.Threading.Tasks;

namespace XTI_TempLog.Abstractions
{
    public interface IPermanentLogClient
    {
        Task StartSession(IStartSessionModel model);
        Task StartRequest(IStartRequestModel model);
        Task EndRequest(IEndRequestModel model);
        Task EndSession(IEndSessionModel model);
        Task AuthenticateSession(IAuthenticateSessionModel model);
        Task LogEvent(ILogEventModel model);
        Task LogBatch(ILogBatchModel model);
    }
}
