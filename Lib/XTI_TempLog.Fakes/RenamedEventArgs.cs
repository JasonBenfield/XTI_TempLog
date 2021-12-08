namespace XTI_TempLog.Fakes;

public sealed class RenamedEventArgs
{
    public RenamedEventArgs(FakeTempLogFile oldFile, FakeTempLogFile newFile)
    {
        OldFile = oldFile;
        NewFile = newFile;
    }

    public FakeTempLogFile OldFile { get; }
    public FakeTempLogFile NewFile { get; }
}
