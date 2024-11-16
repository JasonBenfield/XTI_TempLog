using XTI_TempLog.Abstractions;

namespace XTI_TempLog;

public abstract class TempLog
{
    protected TempLog() { }

    public ITempLogFile[] Files(DateTimeOffset modifiedUntil, int count = 1000) =>
        FileNames("*.log")
            .OrderBy(fn => fn)
            .Take(count)
            .Select(f => CreateFile(f))
            .Where(f => f.LastModified <= modifiedUntil)
            .ToArray();

    public ITempLogFile[] FilesInProgress(DateTimeOffset modifiedUntil) =>
        FileNames("*.processing")
            .OrderBy(fn => fn)
            .Select(f => CreateFile(f))
            .Where(f => f.LastModified <= modifiedUntil)
            .ToArray();

    protected abstract IEnumerable<string> FileNames(string pattern);

    internal Task Write(string fileName, TempLogSessionDetailModel[] sessionDetails) =>
        CreateFile(fileName).Write(sessionDetails);

    protected abstract ITempLogFile CreateFile(string name);
}