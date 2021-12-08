namespace XTI_TempLog;

public sealed class NextThrottledIntervalBuilder
{
    private readonly ThrottledIntervalBuilder builder;
    private readonly double quantity;

    public NextThrottledIntervalBuilder(ThrottledIntervalBuilder builder, double quantity)
    {
        this.builder = builder;
        this.quantity = quantity;
    }

    public ThrottledPathBuilder Milliseconds()
        => setTimeSpan(TimeSpan.FromMilliseconds(quantity));

    public ThrottledPathBuilder Seconds()
        => setTimeSpan(TimeSpan.FromSeconds(quantity));

    public ThrottledPathBuilder Minutes()
        => setTimeSpan(TimeSpan.FromMinutes(quantity));

    public ThrottledPathBuilder Hours()
        => setTimeSpan(TimeSpan.FromHours(quantity));

    private ThrottledPathBuilder setTimeSpan(TimeSpan ts) => builder.For(ts);
}