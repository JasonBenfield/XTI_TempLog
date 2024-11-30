using XTI_Core;

namespace XTI_TempLog;

public sealed class ThrottledLog
{
    private readonly IClock clock;
    private readonly int throttleLogInterval;
    private readonly int throttleExceptionInterval;
    private DateTimeOffset timeRequestLogged = DateTimeOffset.MinValue;
    private DateTimeOffset timeExceptionLogged = DateTimeOffset.MinValue;
    private readonly ThrottledLogPath throttledPath;

    internal ThrottledLog(ThrottledLogPath throttledPath, IClock clock)
    {
        this.throttledPath = throttledPath;
        throttleLogInterval = throttledPath.ThrottleRequestInterval;
        throttleExceptionInterval = throttledPath.ThrottleExceptionInterval;
        this.clock = clock;
    }

    public bool IsForPath(string path) => throttledPath.IsForPath(path);

    public int RequestCount { get; private set; }

    public void IncrementRequestCount() => RequestCount++;

    public int ExceptionCount { get; private set; }

    public void IncrementExceptionCount() => ExceptionCount++;

    public bool CanLogRequest()
    {
        if (throttleLogInterval > 0 && timeRequestLogged > DateTimeOffset.MinValue)
        {
            var now = clock.Now();
            var nextLogTime = timeRequestLogged.AddMilliseconds(throttleLogInterval);
            return now > nextLogTime;
        }
        return true;
    }

    public void RequestLogged()
    {
        timeRequestLogged = clock.Now();
        RequestCount = 0;
    }

    public bool CanLogException()
    {
        if (throttleExceptionInterval > 0 && timeExceptionLogged > DateTimeOffset.MinValue)
        {
            var now = clock.Now();
            var nextLogTime = timeExceptionLogged.AddMilliseconds(throttleExceptionInterval);
            return now > nextLogTime;
        }
        return true;
    }

    public void ExceptionLogged()
    {
        timeExceptionLogged = clock.Now();
        ExceptionCount = 0;
    }
}