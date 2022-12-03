using System.Collections.Concurrent;
using XTI_Core;

namespace XTI_TempLog;

public sealed class ThrottledLogs
{
    private readonly IClock clock;
    private readonly ConcurrentDictionary<string, ThrottledLog> throttledLogs = new();

    internal ThrottledLogs(IClock clock, ThrottledPath[] throttles)
    {
        this.clock = clock;
        foreach (var throttle in throttles)
        {
            throttledLogs.AddOrUpdate
            (
                throttle.Path,
                new ThrottledLog
                (
                    new ThrottledPath(throttle.Path, throttle.ThrottleRequestInterval, throttle.ThrottleExceptionInterval),
                    clock
                ),
                (key, tl) => new ThrottledLog
                (
                    new ThrottledPath(throttle.Path, throttle.ThrottleRequestInterval, throttle.ThrottleExceptionInterval),
                    clock
                )
            );
        }
    }

    internal ThrottledLog GetThrottledLog(string path)
    {
        path = path?.ToLower().Trim() ?? "";
        if (!throttledLogs.TryGetValue(path, out var throttledLog))
        {
            throttledLog = throttledLogs.Values.FirstOrDefault(t => t.IsForPath(path));
            if (throttledLog == null)
            {
                throttledLog = new ThrottledLog
                (
                    new ThrottledPath(new TempLogThrottleOptions(path)),
                    clock
                );
            }
            else
            {
                throttledLogs.TryAdd(path, throttledLog);
            }
        }
        return throttledLog;
    }
}