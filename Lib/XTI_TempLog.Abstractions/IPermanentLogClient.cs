namespace XTI_TempLog.Abstractions;

public interface IPermanentLogClient
{
    Task LogBatch(ILogBatchModel model);
}