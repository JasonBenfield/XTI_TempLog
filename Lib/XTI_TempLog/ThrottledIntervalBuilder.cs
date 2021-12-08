namespace XTI_TempLog;

public sealed class ThrottledIntervalBuilder
{
    private readonly ThrottledPathBuilder builder;
    private readonly Action<ThrottledPathBuilder, TimeSpan> execute;

    public ThrottledIntervalBuilder(ThrottledPathBuilder builder, Action<ThrottledPathBuilder, TimeSpan> execute)
    {
        this.builder = builder;
        this.execute = execute;
    }

    public ThrottledPathBuilder For(TimeSpan ts)
    {
        execute(builder, ts);
        return builder;
    }

    public NextThrottledIntervalBuilder For(double quantity) => nextBuilder(quantity);

    public ThrottledPathBuilder ForOneMillisecond() => nextBuilder(1).Milliseconds();

    public ThrottledPathBuilder ForOneSecond() => nextBuilder(1).Seconds();

    public ThrottledPathBuilder ForOneMinute() => nextBuilder(1).Minutes();

    public ThrottledPathBuilder ForOneHour() => nextBuilder(1).Hours();

    private NextThrottledIntervalBuilder nextBuilder(double quantity)
        => new NextThrottledIntervalBuilder(this, quantity);
}
