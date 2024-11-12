using Microsoft.AspNetCore.DataProtection;

namespace XTI_TempLog.Extensions;

public sealed class DiskTempLogV1 : TempLogV1
{
    private readonly IDataProtector dataProtector;
    private readonly string directoryPath;

    public DiskTempLogV1(IDataProtector dataProtector, string directoryPath)
    {
        this.dataProtector = dataProtector;
        this.directoryPath = directoryPath;
    }

    protected override ITempLogFileV1 CreateFile(string name)
    {
        var path = Path.Combine(directoryPath, name);
        return new EncryptedTempLogFileV1(new DiskTempLogFile(path), dataProtector);
    }

    protected override IEnumerable<string> FileNames(string pattern) => 
        Directory.Exists(directoryPath) ? 
            Directory.GetFiles(directoryPath, pattern) : 
            [];
}