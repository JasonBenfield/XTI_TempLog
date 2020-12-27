using System;
using System.IO;
using System.Threading.Tasks;

namespace XTI_TempLog.Extensions
{
    public sealed class DiskTempLogFile : ITempLogFile
    {
        private readonly string path;

        internal DiskTempLogFile(string path)
        {
            this.path = path;
            Name = Path.GetFileName(path);
            LastModified = new FileInfo(path).LastWriteTimeUtc;
        }

        public string Name { get; }

        public DateTimeOffset LastModified { get; }

        public ITempLogFile WithNewName(string name)
        {
            var newPath = Path.Combine(Path.GetDirectoryName(path), name);
            File.Move(path, newPath);
            return new DiskTempLogFile(newPath);
        }

        public void Delete() => File.Delete(path);

        public async Task<string> Read()
        {
            using var reader = new StreamReader(path);
            var content = await reader.ReadToEndAsync();
            return content;
        }

        public async Task Write(string contents)
        {
            var dir = Path.GetDirectoryName(path);
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            using (var writer = new StreamWriter(path))
            {
                await writer.WriteAsync(contents);
            }
        }
    }
}
