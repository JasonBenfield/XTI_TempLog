using System.Text.RegularExpressions;

namespace XTI_TempLog;

public sealed class ThrottledPath
{
    private readonly Regex pathRegex;

    public ThrottledPath(TempLogThrottleOptions options)
        : this(options.Path, options.ThrottleRequestInterval, options.ThrottleExceptionInterval)
    {
    }

    public ThrottledPath(string path, int throttleRequestInterval, int throttleExceptionInterval)
    {
        path = path?.Trim() ?? "";
        if (path.Contains("/") && !path.Contains("\\/"))
        {
            path = path.Replace("/", "\\/");
        }
        pathRegex = new Regex($"{path}", RegexOptions.IgnoreCase);
        ThrottleRequestInterval = throttleRequestInterval;
        ThrottleExceptionInterval = throttleExceptionInterval;
    }

    public int ThrottleRequestInterval { get; }
    public int ThrottleExceptionInterval { get; }

    public bool IsForPath(string path) => pathRegex.IsMatch(path);

    internal string Format() => pathRegex.ToString();
}