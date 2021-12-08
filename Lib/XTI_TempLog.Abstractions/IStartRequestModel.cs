namespace XTI_TempLog.Abstractions;

public interface IStartRequestModel
{
    string RequestKey { get; set; }
    string SessionKey { get; set; }
    public string AppType { get; set; }
    string Path { get; set; }
    DateTimeOffset TimeStarted { get; set; }
    int ActualCount { get; set; }
}