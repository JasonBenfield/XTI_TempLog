using XTI_TempLog.Abstractions;

namespace XTI_TempLog;

internal sealed class FlattenedLogEntryModel
{
    public TempLogSessionModel Session { get; set; } = new();
    public TempLogRequestModel Request { get; set; } = new();
    public LogEntryModel LogEntry { get; set; } = new();
}
