namespace XTI_TempLog;

public abstract class TempLogV1
{
    protected TempLogV1() { }

    public IEnumerable<ITempLogFileV1> StartSessionFiles(DateTimeOffset modifiedUntil) => Files(FileNames("startSession.*.log"), modifiedUntil);
    public IEnumerable<ITempLogFileV1> StartRequestFiles(DateTimeOffset modifiedUntil) => Files(FileNames("startRequest.*.log"), modifiedUntil);
    public IEnumerable<ITempLogFileV1> EndRequestFiles(DateTimeOffset modifiedUntil) => Files(FileNames("endRequest.*.log"), modifiedUntil);
    public IEnumerable<ITempLogFileV1> AuthSessionFiles(DateTimeOffset modifiedUntil) => Files(FileNames("authSession.*.log"), modifiedUntil);
    public IEnumerable<ITempLogFileV1> EndSessionFiles(DateTimeOffset modifiedUntil) => Files(FileNames("endSession.*.log"), modifiedUntil);
    public IEnumerable<ITempLogFileV1> LogEventFiles(DateTimeOffset modifiedUntil) => Files(FileNames("event.*.log"), modifiedUntil);
    public IEnumerable<ITempLogFileV1> ProcessingFiles(DateTimeOffset modifiedUntil) => Files(FileNames("*.processing"), modifiedUntil);

    private IEnumerable<ITempLogFileV1> Files(IEnumerable<string> fileNames, DateTimeOffset modifiedUntil)
        => fileNames
            .Select(f => CreateFile(f))
            .Where(f => f.LastModified <= modifiedUntil);

    protected abstract IEnumerable<string> FileNames(string pattern);

    internal Task Write(string fileName, string contents) => CreateFile(fileName).Write(contents);

    protected abstract ITempLogFileV1 CreateFile(string name);
}