namespace XTI_TempLog.Abstractions;

public sealed class EndSessionModel 
{
    private string sessionKey = "";

    public string SessionKey
    {
        get => sessionKey;
        set => sessionKey = value ?? "";
    }

    public DateTimeOffset TimeEnded { get; set; }
}