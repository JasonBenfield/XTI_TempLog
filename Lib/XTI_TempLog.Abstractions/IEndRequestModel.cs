using System;

namespace XTI_TempLog.Abstractions
{
    public interface IEndRequestModel
    {
        string RequestKey { get; set; }
        DateTime TimeEnded { get; set; }
    }
}
