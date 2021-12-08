namespace XTI_TempLog.Abstractions;

public interface IEndRequestModel
{
    string RequestKey { get; set; }
    DateTimeOffset TimeEnded { get; set; }
}