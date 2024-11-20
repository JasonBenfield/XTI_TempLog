namespace XTI_TempLog;

public sealed class TempLogOptions
{
    public static readonly string TempLog = nameof(TempLog);

    public int WriteIntervalInSeconds { get; set; } = 60;
    public int WaitForWriteInSeconds { get; set; } = 15;
    public TempLogThrottleOptions[] Throttles { get; set; } = [];
}
