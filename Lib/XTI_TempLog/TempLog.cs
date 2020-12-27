using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace XTI_TempLog
{
    public abstract class TempLog
    {
        protected TempLog() { }

        public IEnumerable<ITempLogFile> StartSessionFiles(DateTimeOffset modifiedUntil) => Files(FileNames("startSession.*.log"), modifiedUntil);
        public IEnumerable<ITempLogFile> StartRequestFiles(DateTimeOffset modifiedUntil) => Files(FileNames("startRequest.*.log"), modifiedUntil);
        public IEnumerable<ITempLogFile> EndRequestFiles(DateTimeOffset modifiedUntil) => Files(FileNames("endRequest.*.log"), modifiedUntil);
        public IEnumerable<ITempLogFile> AuthSessionFiles(DateTimeOffset modifiedUntil) => Files(FileNames("authSession.*.log"), modifiedUntil);
        public IEnumerable<ITempLogFile> EndSessionFiles(DateTimeOffset modifiedUntil) => Files(FileNames("endSession.*.log"), modifiedUntil);
        public IEnumerable<ITempLogFile> LogEventFiles(DateTimeOffset modifiedUntil) => Files(FileNames("event.*.log"), modifiedUntil);
        public IEnumerable<ITempLogFile> ProcessingFiles(DateTimeOffset modifiedUntil) => Files(FileNames("*.processing"), modifiedUntil);

        private IEnumerable<ITempLogFile> Files(IEnumerable<string> fileNames, DateTimeOffset modifiedUntil)
            => fileNames
                .Select(f => CreateFile(f))
                .Where(f => f.LastModified <= modifiedUntil);

        protected abstract IEnumerable<string> FileNames(string pattern);

        internal Task Write(string fileName, string contents) => CreateFile(fileName).Write(contents);

        protected abstract ITempLogFile CreateFile(string name);
    }
}
