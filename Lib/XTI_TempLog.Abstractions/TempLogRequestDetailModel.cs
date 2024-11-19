namespace XTI_TempLog.Abstractions;

public sealed class TempLogRequestDetailModel
{
    public TempLogRequestModel Request { get; set; } = new();
    public LogEntryModel[] LogEntries { get; set; } = [];
}
