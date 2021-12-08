using XTI_TempLog.Abstractions;

namespace XTI_TempLog;

public sealed class StartRequestModel : IStartRequestModel
{
    public string RequestKey { get; set; } = "";
    public string SessionKey { get; set; } = "";
    public string AppType { get; set; } = "";
    public string Path { get; set; } = "";
    public DateTimeOffset TimeStarted { get; set; }
    public int ActualCount { get; set; }
}
