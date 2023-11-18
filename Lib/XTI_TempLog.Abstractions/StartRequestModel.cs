namespace XTI_TempLog.Abstractions;

public sealed class StartRequestModel 
{
    public string RequestKey { get; set; } = "";
    public string SessionKey { get; set; } = "";
    public string SourceRequestKey { get; set; } = "";
    public string Path { get; set; } = "";
    public int InstallationID { get; set; }
    public DateTimeOffset TimeStarted { get; set; }
    public int ActualCount { get; set; }
}
