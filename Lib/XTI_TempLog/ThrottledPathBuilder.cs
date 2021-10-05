using System;

namespace XTI_TempLog
{
    public sealed class ThrottledPathBuilder
    {
        private string path;

        public string Path() => path;

        public ThrottledPathBuilder Path(string path)
        {
            this.path = path?.Trim() ?? "";
            return this;
        }

        private TimeSpan requestInterval = new TimeSpan();

        public ThrottledIntervalBuilder Requests()
            => new ThrottledIntervalBuilder(this, (b, ts) => requestInterval = ts);

        private TimeSpan exceptionInterval = new TimeSpan();

        public ThrottledIntervalBuilder Exceptions()
            => new ThrottledIntervalBuilder(this, (b, ts) => exceptionInterval = ts);

        public ThrottledPath Build()
            => new ThrottledPath
            (
                path,
                (int)requestInterval.TotalMilliseconds,
                (int)exceptionInterval.TotalMilliseconds
            );
    }

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
}
