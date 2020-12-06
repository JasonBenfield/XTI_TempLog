using System;
using XTI_TempLog.Abstractions;

namespace XTI_TempLog
{
    public sealed class EndRequestModel : IEndRequestModel
    {
        public string RequestKey { get; set; }
        public DateTime TimeEnded { get; set; }
    }
}
