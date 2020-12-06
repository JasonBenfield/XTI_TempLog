using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace XTI_TempLog
{
    public abstract class TempLog
    {
        protected TempLog() { }

        public IEnumerable<ITempLogFile> StartSessionFiles(DateTime modifiedUntil) => Files(FileNames("startSession.*.log"), modifiedUntil);
        public IEnumerable<ITempLogFile> StartRequestFiles(DateTime modifiedUntil) => Files(FileNames("startRequest.*.log"), modifiedUntil);
        public IEnumerable<ITempLogFile> EndRequestFiles(DateTime modifiedUntil) => Files(FileNames("endRequest.*.log"), modifiedUntil);
        public IEnumerable<ITempLogFile> AuthSessionFiles(DateTime modifiedUntil) => Files(FileNames("authSession.*.log"), modifiedUntil);
        public IEnumerable<ITempLogFile> EndSessionFiles(DateTime modifiedUntil) => Files(FileNames("endSession.*.log"), modifiedUntil);
        public IEnumerable<ITempLogFile> LogEventFiles(DateTime modifiedUntil) => Files(FileNames("event.*.log"), modifiedUntil);
        public IEnumerable<ITempLogFile> ProcessingFiles(DateTime modifiedUntil) => Files(FileNames("*.processing"), modifiedUntil);

        private IEnumerable<ITempLogFile> Files(IEnumerable<string> fileNames, DateTime modifiedUntil)
            => fileNames
                .Select(f => CreateFile(f))
                .Where(f => f.LastModified.ToUniversalTime() <= modifiedUntil.ToUniversalTime());

        protected abstract IEnumerable<string> FileNames(string pattern);

        internal Task Write(string fileName, string contents) => CreateFile(fileName).Write(contents);

        protected abstract ITempLogFile CreateFile(string name);
    }
}
