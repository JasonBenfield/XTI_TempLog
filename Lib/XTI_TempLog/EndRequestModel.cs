using XTI_TempLog.Abstractions;

namespace XTI_TempLog;

public sealed class EndRequestModel : IEndRequestModel
{
    private string requestKey = "";

    public string RequestKey
    {
        get => requestKey;
        set => requestKey = value ?? "";
    }

    public DateTimeOffset TimeEnded { get; set; }
}