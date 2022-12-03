namespace XTI_TempLog;

public sealed class TempLogThrottleOptions
{
    public TempLogThrottleOptions()
        : this("")
    {
    }

    public TempLogThrottleOptions(string path)
    {
        Path = path;
    }

    public string Path { get; set; }
    public int ThrottleRequestInterval { get; set; }
    public int ThrottleExceptionInterval { get; set; }
}