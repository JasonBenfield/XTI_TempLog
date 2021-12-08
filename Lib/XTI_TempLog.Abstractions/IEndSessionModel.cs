namespace XTI_TempLog.Abstractions;

public interface IEndSessionModel
{
    string SessionKey { get; set; }
    DateTimeOffset TimeEnded { get; set; }
}