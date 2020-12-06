using Microsoft.AspNetCore.DataProtection;
using System;
using System.Threading.Tasks;
using XTI_Secrets;

namespace XTI_TempLog.Extensions
{
    public sealed class EncryptedTempLogFile : ITempLogFile
    {
        private readonly ITempLogFile source;
        private readonly IDataProtector dataProtector;

        public EncryptedTempLogFile(ITempLogFile source, IDataProtector dataProtector)
        {
            this.source = source;
            this.dataProtector = dataProtector;
        }

        public string Name { get => source.Name; }

        public DateTime LastModified { get => source.LastModified; }

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

        public ITempLogFile WithNewName(string name) => new EncryptedTempLogFile(source.WithNewName(name), dataProtector);

        public void Delete() => source.Delete();

    }
}
