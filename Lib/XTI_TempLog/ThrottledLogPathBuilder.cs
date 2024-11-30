namespace XTI_TempLog;

public sealed class ThrottledLogPathBuilder
{
    private string path = "";
    private readonly ThrottledLogsBuilder builder;
    private readonly ThrottledLogIntervalBuilder requestIntervalBuilder;
    private readonly ThrottledLogIntervalBuilder exceptionIntervalBuilder;

    internal ThrottledLogPathBuilder(ThrottledLogsBuilder builder)
    {
        this.builder = builder;
        requestIntervalBuilder = new(this);
        exceptionIntervalBuilder = new(this);
    }

    internal string Path() => path;

    internal ThrottledLogPathBuilder Path(string path)
    {
        this.path = path;
        return this;
    }

    public ThrottledLogIntervalBuilder Requests() => requestIntervalBuilder;

    public ThrottledLogIntervalBuilder Exceptions() => exceptionIntervalBuilder;

    internal ThrottledLogPath Build() =>
        new
        (
            path: path,
            throttleRequestInterval: requestIntervalBuilder.Interval,
            throttleExceptionInterval: exceptionIntervalBuilder.Interval
        );

    public ThrottledLogPathBuilder AndThrottle(string path) => builder.Throttle(path);
}
