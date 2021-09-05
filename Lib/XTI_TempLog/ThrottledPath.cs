using System.Text.RegularExpressions;

namespace XTI_TempLog
{
    public sealed class ThrottledPath
    {
        private readonly Regex pathRegex;

        public ThrottledPath(TempLogThrottleOptions options)
        {
            var path = options.Path ?? "";
            if (path.Contains("/") && !path.Contains("\\/"))
            {
                path = path.Replace("/", "\\/");
            }
            pathRegex = new Regex($"{path}", RegexOptions.IgnoreCase);
            ThrottleRequestInterval = options.ThrottleRequestInterval;
            ThrottleExceptionInterval = options.ThrottleExceptionInterval;
        }

        public int ThrottleRequestInterval { get; }
        public int ThrottleExceptionInterval { get; }

        public bool IsForPath(string path) => pathRegex.IsMatch(path);
    }
}
