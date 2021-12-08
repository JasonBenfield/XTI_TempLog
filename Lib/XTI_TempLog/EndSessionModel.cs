using XTI_TempLog.Abstractions;

namespace XTI_TempLog;

public sealed class EndSessionModel : IEndSessionModel
{
    private string sessionKey = "";

    public string SessionKey
    {
        get => sessionKey;
        set => sessionKey = value ?? "";
    }

    public DateTimeOffset TimeEnded { get; set; }
}