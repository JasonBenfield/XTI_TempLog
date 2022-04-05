namespace XTI_TempLog.Abstractions;

public sealed class EndRequestModel
{
    private string requestKey = "";

    public string RequestKey
    {
        get => requestKey;
        set => requestKey = value ?? "";
    }

    public DateTimeOffset TimeEnded { get; set; }
}