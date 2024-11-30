using XTI_Core;

namespace XTI_TempLog;

public sealed class ThrottledLogsBuilder
{
    private readonly IClock clock;
    private readonly List<ThrottledLogPathBuilder> throttles = new();

    public ThrottledLogsBuilder(IClock clock)
    {
        this.clock = clock;
    }

    public ThrottledLogPathBuilder Throttle(string path)
    {
        throttles.RemoveAll(t => t.Path().Equals(path, StringComparison.OrdinalIgnoreCase));
        var throttle = new ThrottledLogPathBuilder(this).Path(path);
        throttles.Add(throttle);
        return throttle;
    }

    public ThrottledLogsBuilder ApplyOptions(TempLogOptions options)
    {
        var throttles = options?.Throttles ?? [];
        foreach (var throttleOption in throttles)
        {
            Throttle(throttleOption.Path)
                .Requests().For(throttleOption.ThrottleRequestInterval).Milliseconds()
                .Exceptions().For(throttleOption.ThrottleExceptionInterval).Milliseconds();
        }
        return this;
    }

    public ThrottledLogsBuilder ApplyThrottledPaths(ThrottledLogPath[] throttledLogPaths)
    {
        throttledLogPaths = throttledLogPaths.Where(tlp => tlp.IsThrottled()).ToArray();
        foreach (var throttledLogPath in throttledLogPaths)
        {
            Throttle(throttledLogPath.Path)
                .Requests().For(throttledLogPath.ThrottleRequestInterval).Milliseconds()
                .Exceptions().For(throttledLogPath.ThrottleExceptionInterval).Milliseconds();
        }
        return this;
    }

    public ThrottledLogs Build() => new(clock, throttles.Select(t => t.Build()).ToArray());
}