namespace XTI_TempLog.Abstractions;

public sealed class TempLogSessionModel
{
    public SessionKey SessionKey { get; set; } = new();
    public string RequesterKey { get; set; } = "";
    public DateTimeOffset TimeStarted { get; set; } = DateTimeOffset.MaxValue;
    public DateTimeOffset TimeEnded { get; set; } = DateTimeOffset.MaxValue;
    public string RemoteAddress { get; set; } = "";
    public string UserAgent { get; set; } = "";
}
