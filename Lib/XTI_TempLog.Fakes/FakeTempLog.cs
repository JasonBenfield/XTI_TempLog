using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using XTI_Core;

namespace XTI_TempLog.Fakes
{
    public sealed class FakeTempLog : TempLog
    {
        private readonly Dictionary<string, FakeTempLogFile> files = new Dictionary<string, FakeTempLogFile>();
        private readonly Clock clock;

        public FakeTempLog(Clock clock)
        {
            this.clock = clock;
        }

        public string[] Files() => files.Keys.ToArray();

        protected override ITempLogFile CreateFile(string name)
        {
            var key = getLookupKey(name);
            if (!files.TryGetValue(key, out var file))
            {
                var lastModified = clock.Now();
                file = new FakeTempLogFile(name, lastModified);
                addFile(key, file);
            }
            return file;
        }

        private void File_Renamed(object sender, RenamedEventArgs e)
        {
            files.Remove(getLookupKey(e.OldFile.Name));
            var key = getLookupKey(e.NewFile.Name);
            addFile(key, e.NewFile);
        }

        private void addFile(string key, FakeTempLogFile file)
        {
            file.Renamed += File_Renamed;
            file.Deleted += File_Deleted;
            files.Add(key, file);
        }

        private void File_Deleted(object sender, System.EventArgs e)
        {
            var file = (FakeTempLogFile)sender;
            files.Remove(getLookupKey(file.Name));
        }

        private static string getLookupKey(string name) => name.ToLower();

        protected override IEnumerable<string> FileNames(string pattern)
            => files.Keys.Where
            (
                key => new Regex(pattern.Replace("*", ".*"), RegexOptions.IgnoreCase).IsMatch(key)
            )
            .ToArray();
    }
}
