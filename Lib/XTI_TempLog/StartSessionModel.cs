using System;
using XTI_TempLog.Abstractions;

namespace XTI_TempLog
{
    public sealed class StartSessionModel : IStartSessionModel
    {
        public string SessionKey { get; set; }
        public string UserName { get; set; }
        public string RequesterKey { get; set; }
        public DateTimeOffset TimeStarted { get; set; }
        public string RemoteAddress { get; set; }
        public string UserAgent { get; set; }
    }
}
