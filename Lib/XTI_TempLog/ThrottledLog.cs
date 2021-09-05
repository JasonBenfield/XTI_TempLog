using System;
using XTI_Core;

namespace XTI_TempLog
{
    internal sealed class ThrottledLog
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

        public void RequestLogged() => timeLastRequestLogged = clock.Now();

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

        public void ExceptionLogged() => timeLastExceptionLogged = clock.Now();

    }

}
