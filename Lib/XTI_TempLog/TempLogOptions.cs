namespace XTI_TempLog;

public sealed class TempLogOptions
{
    public static readonly string TempLog = nameof(TempLog);
    public TempLogThrottleOptions[] Throttles { get; set; } = new TempLogThrottleOptions[0];
}
