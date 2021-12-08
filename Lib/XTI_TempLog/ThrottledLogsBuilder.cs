using XTI_Core;

namespace XTI_TempLog;

public sealed class ThrottledLogsBuilder
{
    private readonly IClock clock;
    private readonly List<ThrottledPathBuilder> throttles = new List<ThrottledPathBuilder>();

    public ThrottledLogsBuilder(IClock clock)
    {
        this.clock = clock;
    }

    public ThrottledPathBuilder Throttle(string path)
    {
        throttles.RemoveAll(t => t.Path().Equals(path, StringComparison.OrdinalIgnoreCase));
        var throttle = new ThrottledPathBuilder(this).Path(path);
        throttles.Add(throttle);
        return throttle;
    }

    public ThrottledLogsBuilder ApplyOptions(TempLogOptions options)
    {
        var throttles = options?.Throttles ?? new TempLogThrottleOptions[] { };
        foreach (var throttleOption in throttles)
        {
            Throttle(throttleOption.Path)
                .Requests().For(throttleOption.ThrottleRequestInterval).Milliseconds()
                .Exceptions().For(throttleOption.ThrottleExceptionInterval).Milliseconds();
        }
        return this;
    }

    public ThrottledLogs Build()
        => new ThrottledLogs(clock, throttles.Select(t => t.Build()).ToArray());
}