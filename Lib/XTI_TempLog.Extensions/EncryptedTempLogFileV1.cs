using Microsoft.AspNetCore.DataProtection;
using XTI_Secrets;

namespace XTI_TempLog.Extensions;

public sealed class EncryptedTempLogFileV1 : ITempLogFileV1
{
    private readonly ITempLogFileV1 source;
    private readonly IDataProtector dataProtector;

    public EncryptedTempLogFileV1(ITempLogFileV1 source, IDataProtector dataProtector)
    {
        this.source = source;
        this.dataProtector = dataProtector;
    }

    public string Name { get => source.Name; }

    public DateTimeOffset LastModified { get => source.LastModified; }

    public Task Write(string contents)
    {
        var encryptedValue = new EncryptedValue(dataProtector, contents);
        return source.Write(encryptedValue.Value());
    }

    public async Task<string> Read()
    {
        var contents = await source.Read();
        var encryptedValue = new DecryptedValue(dataProtector, contents);
        return encryptedValue.Value();
    }

    public ITempLogFileV1 WithNewName(string name) => new EncryptedTempLogFileV1(source.WithNewName(name), dataProtector);

    public void Delete() => source.Delete();
}