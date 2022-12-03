using System.Text.RegularExpressions;

namespace XTI_TempLog;

public sealed partial class ThrottledPath
{
    private readonly Regex pathRegex;

    public ThrottledPath(TempLogThrottleOptions options)
        : this(options.Path, options.ThrottleRequestInterval, options.ThrottleExceptionInterval)
    {
    }

    public ThrottledPath(string path, int throttleRequestInterval, int throttleExceptionInterval)
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
    public int ThrottleRequestInterval { get; }
    public int ThrottleExceptionInterval { get; }

    public bool IsForPath(string path) => pathRegex.IsMatch(path);

    internal string Format() => pathRegex.ToString();

    [GeneratedRegex("\\s+")]
    private static partial Regex WhitespaceRegex();
}