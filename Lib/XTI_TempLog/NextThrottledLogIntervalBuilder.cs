namespace XTI_TempLog;

public sealed class NextThrottledLogIntervalBuilder
{
    private readonly ThrottledLogIntervalBuilder builder;
    private readonly double quantity;

    internal NextThrottledLogIntervalBuilder(ThrottledLogIntervalBuilder builder, double quantity)
    {
        this.builder = builder;
        this.quantity = quantity;
    }

    public ThrottledLogPathBuilder Milliseconds() => SetTimeSpan(TimeSpan.FromMilliseconds(quantity));

    public ThrottledLogPathBuilder Seconds() => SetTimeSpan(TimeSpan.FromSeconds(quantity));

    public ThrottledLogPathBuilder Minutes() => SetTimeSpan(TimeSpan.FromMinutes(quantity));

    public ThrottledLogPathBuilder Hours() => SetTimeSpan(TimeSpan.FromHours(quantity));

    private ThrottledLogPathBuilder SetTimeSpan(TimeSpan ts) => builder.For(ts);
}
