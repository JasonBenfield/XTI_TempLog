using System;

namespace XTI_TempLog.Abstractions
{
    public interface IStartSessionModel
    {
        string SessionKey { get; set; }
        string UserName { get; set; }
        string RequesterKey { get; set; }
        DateTimeOffset TimeStarted { get; set; }
        string RemoteAddress { get; set; }
        string UserAgent { get; set; }
    }
}
