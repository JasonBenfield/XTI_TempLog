﻿using System;
using XTI_TempLog.Abstractions;

namespace XTI_TempLog
{
    public sealed class EndSessionModel : IEndSessionModel
    {
        public string SessionKey { get; set; }
        public DateTimeOffset TimeEnded { get; set; }
    }
}
