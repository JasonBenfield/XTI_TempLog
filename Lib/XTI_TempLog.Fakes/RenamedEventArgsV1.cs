namespace XTI_TempLog.Fakes;

public sealed class RenamedEventArgsV1
{
    public RenamedEventArgsV1(FakeTempLogFileV1 oldFile, FakeTempLogFileV1 newFile)
    {
        OldFile = oldFile;
        NewFile = newFile;
    }

    public FakeTempLogFileV1 OldFile { get; }
    public FakeTempLogFileV1 NewFile { get; }
}
