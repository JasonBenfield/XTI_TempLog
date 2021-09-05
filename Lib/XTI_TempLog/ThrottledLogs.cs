using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Linq;
using XTI_Core;

namespace XTI_TempLog
{
    public sealed class ThrottledLogs
    {
        private readonly Clock clock;
        private readonly ThrottledPath[] throttles;
        private readonly Dictionary<string, ThrottledLog> throttledLogs = new Dictionary<string, ThrottledLog>();

        public ThrottledLogs(Clock clock, IOptions<TempLogOptions> options)
        {
            this.clock = clock;
            throttles = (options.Value.Throttles ?? new TempLogThrottleOptions[] { })
                .Select(t => new ThrottledPath(t))
                .ToArray();
        }

        internal ThrottledLog GetThrottledLog(string path)
        {
            path = path?.ToLower().Trim() ?? "";
            if (!throttledLogs.TryGetValue(path, out var throttledLog))
            {
                var throttle = throttles
                    .FirstOrDefault(t => t.IsForPath(path))
                    ?? new ThrottledPath(new TempLogThrottleOptions { Path = path });
                throttledLog = new ThrottledLog(throttle, clock);
                throttledLogs.Add(path, throttledLog);
            }
            return throttledLog;
        }

    }
}
