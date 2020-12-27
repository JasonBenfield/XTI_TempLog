using System;

namespace XTI_TempLog.Abstractions
{
    public interface ILogEventModel
    {
        string EventKey { get; set; }
        string RequestKey { get; set; }
        int Severity { get; set; }
        DateTimeOffset TimeOccurred { get; set; }
        string Caption { get; set; }
        string Message { get; set; }
        string Detail { get; set; }
    }
}
