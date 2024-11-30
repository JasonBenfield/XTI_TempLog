namespace XTI_TempLog;

public sealed class ThrottledLogIntervalBuilder
{
    private readonly ThrottledLogPathBuilder builder;

    internal ThrottledLogIntervalBuilder(ThrottledLogPathBuilder builder)
    {
        this.builder = builder;
    }

    internal TimeSpan Interval { get; private set; }

    public ThrottledLogPathBuilder For(TimeSpan ts)
    {
        Interval = ts;
        return builder;
    }

    public NextThrottledLogIntervalBuilder For(double quantity) => NextBuilder(quantity);

    public ThrottledLogPathBuilder ForOneMillisecond() => NextBuilder(1).Milliseconds();

    public ThrottledLogPathBuilder ForOneSecond() => NextBuilder(1).Seconds();

    public ThrottledLogPathBuilder ForOneMinute() => NextBuilder(1).Minutes();

    public ThrottledLogPathBuilder ForOneHour() => NextBuilder(1).Hours();

    private NextThrottledLogIntervalBuilder NextBuilder(double quantity) =>
        new(this, quantity);
}
