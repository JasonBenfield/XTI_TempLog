namespace XTI_TempLog;

public sealed class TempLogThrottleOptions
{
    public string Path { get; set; } = "";
    public int ThrottleRequestInterval { get; set; }
    public int ThrottleExceptionInterval { get; set; }
}