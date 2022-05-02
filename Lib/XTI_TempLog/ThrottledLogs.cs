using System.Collections.Concurrent;
using XTI_Core;

namespace XTI_TempLog;

public sealed class ThrottledLogs
{
    private readonly IClock clock;
    private readonly ThrottledPath[] throttles;
    private readonly ConcurrentDictionary<string, ThrottledLog> throttledLogs = new();

    internal ThrottledLogs(IClock clock, ThrottledPath[] throttles)
    {
        this.clock = clock;
        this.throttles = throttles;
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
            throttledLogs.TryAdd(path, throttledLog);
        }
        return throttledLog;
    }
}