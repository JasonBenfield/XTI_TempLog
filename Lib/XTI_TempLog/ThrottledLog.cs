using System;
using XTI_Core;

namespace XTI_TempLog
{
    public sealed class ThrottledLog
    {
        private readonly Clock clock;
        private readonly int throttleLogInterval;
        private DateTimeOffset timeLastRequestLogged;
        private readonly int throttleExceptionInterval;
        private DateTimeOffset timeLastExceptionLogged;

        public ThrottledLog(ThrottledPath throttledPath, Clock clock)
        {
            throttleLogInterval = throttledPath.ThrottleRequestInterval;
            throttleExceptionInterval = throttledPath.ThrottleExceptionInterval;
            timeLastRequestLogged = DateTimeOffset.MinValue;
            this.clock = clock;
        }

        public int RequestCount { get; private set; }

        public void IncrementRequestCount() => RequestCount++;

        public int ExceptionCount { get; private set; }

        public void IncrementExceptionCount() => ExceptionCount++;

        public bool CanLogRequest()
        {
            if (throttleLogInterval > 0 && timeLastRequestLogged > DateTimeOffset.MinValue)
            {
                var now = clock.Now();
                var nextLogTime = timeLastRequestLogged.AddMilliseconds(throttleLogInterval);
                return now > nextLogTime;
            }
            return true;
        }

        public void RequestLogged()
        {
            timeLastRequestLogged = clock.Now();
            RequestCount = 0;
        }

        public bool CanLogException()
        {
            if (throttleExceptionInterval > 0 && timeLastExceptionLogged > DateTimeOffset.MinValue)
            {
                var now = clock.Now();
                var nextLogTime = timeLastExceptionLogged.AddMilliseconds(throttleExceptionInterval);
                return now > nextLogTime;
            }
            return true;
        }

        public void ExceptionLogged()
        {
            timeLastExceptionLogged = clock.Now();
            ExceptionCount = 0;
        }

    }

}
