using XTI_TempLog.Abstractions;

namespace XTI_TempLog;

public sealed class LogEventModel : ILogEventModel
{
    private string eventKey = "";
    private string requestKey = "";
    private string caption = "";
    private string message = "";
    private string detail = "";

    public string EventKey
    {
        get => eventKey;
        set => eventKey = value ?? "";
    }

    public string RequestKey
    {
        get => requestKey;
        set => requestKey = value ?? "";
    }

    public int Severity { get; set; }

    public DateTimeOffset TimeOccurred { get; set; }

    public string Caption
    {
        get => caption;
        set => caption = value ?? "";
    }

    public string Message
    {
        get => message;
        set => message = value ?? "";
    }

    public string Detail
    {
        get => detail;
        set => detail = value ?? "";
    }

    public int ActualCount { get; set; }
}