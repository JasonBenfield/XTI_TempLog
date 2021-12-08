namespace XTI_TempLog;

public interface ITempLogFile
{
    string Name { get; }
    DateTimeOffset LastModified { get; }
    ITempLogFile WithNewName(string name);
    Task Write(string contents);
    Task<string> Read();
    void Delete();
}