using Microsoft.AspNetCore.DataProtection;
using XTI_Core;
using XTI_Secrets;
using XTI_TempLog.Abstractions;

namespace XTI_TempLog.Extensions;

public sealed class EncryptedTempLogFile : ITempLogFile
{
    private readonly DiskTempLogFile sourceFile;
    private readonly IDataProtector dataProtector;

    public EncryptedTempLogFile(DiskTempLogFile sourceFile, IDataProtector dataProtector)
    {
        this.sourceFile = sourceFile;
        this.dataProtector = dataProtector;
    }

    public string Name { get => sourceFile.Name; }

    public DateTimeOffset LastModified { get => sourceFile.LastModified; }

    public Task Write(TempLogSessionDetailModel[] sessionDetails)
    {
        var encryptedValue = new EncryptedValue(dataProtector, XtiSerializer.Serialize(sessionDetails));
        return sourceFile.WriteFile(encryptedValue.Value());
    }

    public async Task<TempLogSessionDetailModel[]> Read()
    {
        var contents = await sourceFile.ReadFile();
        var decryptedValue = new DecryptedValue(dataProtector, contents);
        var decryptedContents = decryptedValue.Value();
        var sessionDetails =
            string.IsNullOrWhiteSpace(decryptedContents) ?
                [] :
                XtiSerializer.DeserializeArray<TempLogSessionDetailModel>(decryptedContents);
        return sessionDetails;
    }

    public ITempLogFile WithNewName(string name) => 
        new EncryptedTempLogFile(sourceFile.WithNewName(name), dataProtector);

    public void Delete() => sourceFile.Delete();
}