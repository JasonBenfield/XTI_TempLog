using System.Text.RegularExpressions;

namespace XTI_TempLog;

public sealed partial class ThrottledLogPath
{
    private readonly Regex pathRegex;

    internal ThrottledLogPath(TempLogThrottleOptions options)
        : this(options.Path, options.ThrottleRequestInterval, options.ThrottleExceptionInterval)
    {
    }

    public ThrottledLogPath(string path, TimeSpan throttleRequestInterval, TimeSpan throttleExceptionInterval)
        : this(path, (int)throttleRequestInterval.TotalMilliseconds, (int)throttleExceptionInterval.TotalMilliseconds)
    {
    }

    internal ThrottledLogPath(string path, int throttleRequestInterval, int throttleExceptionInterval)
    {
        path = WhitespaceRegex().Replace(path?.Trim() ?? "", "");
        Path = path;
        if (path.Contains("/") && !path.Contains("\\/"))
        {
            path = path.Replace("/", "\\/");
        }
        pathRegex = new Regex($"{path}", RegexOptions.IgnoreCase);
        ThrottleRequestInterval = throttleRequestInterval;
        ThrottleExceptionInterval = throttleExceptionInterval;
    }

    internal string Path { get; }
    internal int ThrottleRequestInterval { get; }
    internal int ThrottleExceptionInterval { get; }

    internal bool IsThrottled() => ThrottleRequestInterval > 0 || ThrottleExceptionInterval > 0;

    internal bool IsForPath(string path) => pathRegex.IsMatch(path);

    internal string Format() => pathRegex.ToString();

    [GeneratedRegex("\\s+")]
    private static partial Regex WhitespaceRegex();
}