namespace XTI_TempLog.Abstractions;

public sealed class TempLogRequestModel
{
    public string RequestKey { get; set; } = "";
    public string SourceRequestKey { get; set; } = "";
    public string Path { get; set; } = "";
    public int InstallationID { get; set; }
    public DateTimeOffset TimeStarted { get; set; } = DateTimeOffset.MaxValue;
    public DateTimeOffset TimeEnded { get; set; } = DateTimeOffset.MaxValue;
    public int ActualCount { get; set; }
}
