namespace XTI_TempLog
{
    public sealed class TempLogOptions
    {
        public static readonly string TempLog = nameof(TempLog);
        public TempLogThrottleOptions[] Throttles { get; set; }
    }

    public sealed class TempLogThrottleOptions
    {
        public string Path { get; set; }
        public int ThrottleRequestInterval { get; set; }
        public int ThrottleExceptionInterval { get; set; }
    }
}
