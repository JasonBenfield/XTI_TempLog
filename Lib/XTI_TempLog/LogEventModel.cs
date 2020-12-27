using System;
using XTI_TempLog.Abstractions;

namespace XTI_TempLog
{
    public sealed class LogEventModel : ILogEventModel
    {
        public string EventKey { get; set; }
        public string RequestKey { get; set; }
        public int Severity { get; set; }
        public DateTimeOffset TimeOccurred { get; set; }
        public string Caption { get; set; }
        public string Message { get; set; }
        public string Detail { get; set; }
    }
}
