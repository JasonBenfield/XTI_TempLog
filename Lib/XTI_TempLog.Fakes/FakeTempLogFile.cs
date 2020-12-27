using System;
using System.Threading.Tasks;

namespace XTI_TempLog.Fakes
{
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
    public sealed class FakeTempLogFile : ITempLogFile
    {
        private string contents;

        internal FakeTempLogFile(string name, DateTimeOffset lastModified)
        {
            Name = name;
            LastModified = lastModified;
        }

        public string Name { get; }
        public DateTimeOffset LastModified { get; }

        public event EventHandler<RenamedEventArgs> Renamed;

        public ITempLogFile WithNewName(string name)
        {
            var newFile = new FakeTempLogFile(name, LastModified) { contents = contents };
            Renamed?.Invoke(this, new RenamedEventArgs(this, newFile));
            return newFile;
        }

        public Task<string> Read() => Task.FromResult(contents);

        public Task Write(string contents)
        {
            this.contents = contents;
            return Task.CompletedTask;
        }

        public event EventHandler Deleted;

        public void Delete() => Deleted?.Invoke(this, new EventArgs());

    }
}
