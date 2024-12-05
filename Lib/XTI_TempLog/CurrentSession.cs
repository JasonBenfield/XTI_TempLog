using XTI_TempLog.Abstractions;

namespace XTI_TempLog;

public sealed class CurrentSession
{
    public SessionKey SessionKey { get; set; } = new();
}