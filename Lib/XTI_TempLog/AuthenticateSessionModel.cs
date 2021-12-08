using XTI_TempLog.Abstractions;

namespace XTI_TempLog;

public sealed class AuthenticateSessionModel : IAuthenticateSessionModel
{
    private string sessionKey = "";
    private string userName = "";

    public string SessionKey
    {
        get => sessionKey;
        set => sessionKey = value ?? "";
    }

    public string UserName
    {
        get => userName;
        set => userName = value ?? "";
    }
}