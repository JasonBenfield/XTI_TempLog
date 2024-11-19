namespace XTI_TempLog.Abstractions;

public sealed class TempLogSessionModel
{
    public string SessionKey { get; set; } = "";
    public string UserName { get; set; } = "";
    public string RequesterKey { get; set; } = "";
    public DateTimeOffset TimeStarted { get; set; } = DateTimeOffset.MaxValue;
    public DateTimeOffset TimeEnded { get; set; } = DateTimeOffset.MaxValue;
    public string RemoteAddress { get; set; } = "";
    public string UserAgent { get; set; } = "";
}
