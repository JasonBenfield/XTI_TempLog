using System;

namespace XTI_TempLog.Abstractions
{
    public interface IEndSessionModel
    {
        string SessionKey { get; set; }
        DateTime TimeEnded { get; set; }
    }
}
