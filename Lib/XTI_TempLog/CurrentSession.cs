namespace XTI_TempLog;

public sealed class CurrentSession
{
    private string sessionKey = "";

    public string SessionKey
    {
        get => sessionKey;
        set => sessionKey = value ?? "";
    }
}