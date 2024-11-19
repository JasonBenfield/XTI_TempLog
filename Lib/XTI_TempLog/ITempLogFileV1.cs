namespace XTI_TempLog;

public interface ITempLogFileV1
{
    string Name { get; }
    DateTimeOffset LastModified { get; }
    ITempLogFileV1 WithNewName(string name);
    Task Write(string contents);
    Task<string> Read();
    void Delete();
}