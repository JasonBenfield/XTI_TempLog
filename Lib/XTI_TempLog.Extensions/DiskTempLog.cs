using Microsoft.AspNetCore.DataProtection;

namespace XTI_TempLog.Extensions;

public sealed class DiskTempLog : TempLog
{
    private readonly IDataProtector dataProtector;
    private readonly string directoryPath;

    public DiskTempLog(IDataProtector dataProtector, string directoryPath)
    {
        this.dataProtector = dataProtector;
        this.directoryPath = directoryPath;
    }

    protected override ITempLogFile CreateFile(string name)
    {
        var path = Path.Combine(directoryPath, name);
        return new EncryptedTempLogFile(new DiskTempLogFile(path), dataProtector);
    }

    protected override IEnumerable<string> FileNames(string pattern)
        => Directory.Exists(directoryPath)
            ? Directory.GetFiles(directoryPath, pattern)
            : new string[] { };
}