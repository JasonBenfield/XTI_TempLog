using XTI_TempLog.Abstractions;

namespace XTI_TempLog;

public interface ITempLogFile
{
    string Name { get; }
    DateTimeOffset LastModified { get; }
    ITempLogFile WithNewName(string name);
    Task Write(TempLogSessionDetailModel[] sessionDetails);
    Task<TempLogSessionDetailModel[]> Read();
    void Delete();
}