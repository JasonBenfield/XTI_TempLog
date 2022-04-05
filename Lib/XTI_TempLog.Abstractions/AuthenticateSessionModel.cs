namespace XTI_TempLog.Abstractions;

public sealed class AuthenticateSessionModel
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