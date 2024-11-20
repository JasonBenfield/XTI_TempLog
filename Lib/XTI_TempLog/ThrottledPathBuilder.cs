namespace XTI_TempLog;

public sealed class ThrottledPathBuilder
{
    private readonly ThrottledLogsBuilder builder;
    private string path = "";
    private TimeSpan requestInterval = new();
    private TimeSpan exceptionInterval = new();

    public ThrottledPathBuilder(ThrottledLogsBuilder builder)
    {
        this.builder = builder;
    }

    public string Path() => path;

    public ThrottledPathBuilder Path(string path)
    {
        this.path = path.Trim();
        return this;
    }

    public ThrottledIntervalBuilder Requests()
        => new ThrottledIntervalBuilder(this, (b, ts) => requestInterval = ts);

    public ThrottledIntervalBuilder Exceptions()
        => new ThrottledIntervalBuilder(this, (b, ts) => exceptionInterval = ts);

    internal ThrottledPath Build()
        => new ThrottledPath
        (
            path,
            (int)requestInterval.TotalMilliseconds,
            (int)exceptionInterval.TotalMilliseconds
        );

    public ThrottledPathBuilder AndThrottle(string path)
        => builder.Throttle(path);
}