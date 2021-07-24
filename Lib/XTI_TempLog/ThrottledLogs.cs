using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Linq;
using XTI_Core;

namespace XTI_TempLog
{
    public sealed class ThrottledLogs
    {
        private readonly Clock clock;
        private readonly List<ThrottledLog> throttledLogs = new List<ThrottledLog>();

        public ThrottledLogs(Clock clock, IOptions<TempLogOptions> options)
        {
            this.clock = clock;
            foreach (var throttle in (options.Value.Throttles ?? new TempLogThrottleOptions[] { }))
            {
                throttledLogs.Add(new ThrottledLog(throttle, clock));
            }
        }

        internal ThrottledLog GetThrottledLog(string path)
            => throttledLogs.FirstOrDefault(tl => tl.IsForPath(path))
                ?? new ThrottledLog(new TempLogThrottleOptions { Path = path }, clock);

    }
}
