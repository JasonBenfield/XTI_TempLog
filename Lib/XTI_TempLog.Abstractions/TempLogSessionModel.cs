namespace XTI_TempLog.Abstractions;

public sealed class TempLogSessionModel
{
    public string SessionKey { get; set; } = "";
    public string UserName { get; set; } = "";
    public string RequesterKey { get; set; } = "";
    public DateTimeOffset TimeStarted { get; set; }
    public DateTimeOffset TimeEnded { get; set; }
    public string RemoteAddress { get; set; } = "";
    public string UserAgent { get; set; } = "";
}
