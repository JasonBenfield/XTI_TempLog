using XTI_TempLog.Abstractions;

namespace XTI_TempLog
{
    public sealed class AuthenticateSessionModel : IAuthenticateSessionModel
    {
        public string SessionKey { get; set; }
        public string UserName { get; set; }
    }
}
