using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using XTI_Core;

namespace XTI_TempLog.Fakes;

public sealed class FakeTempLog : TempLog
{
    private readonly ConcurrentDictionary<string, FakeTempLogFile> files = new();
    private readonly IClock clock;

    public FakeTempLog(IClock clock)
    {
        this.clock = clock;
    }

    public string[] Files() => files.Keys.ToArray();

    protected override ITempLogFile CreateFile(string name)
    {
        var key = GetLookupKey(name);
        if (!files.TryGetValue(key, out var file))
        {
            var lastModified = clock.Now();
            file = new FakeTempLogFile(name, lastModified);
            AddFile(key, file);
        }
        return file;
    }

    private void File_Renamed(object? sender, RenamedEventArgs e)
    {
        files.TryRemove(GetLookupKey(e.OldFile.Name), out var _);
        var key = GetLookupKey(e.NewFile.Name);
        AddFile(key, e.NewFile);
    }

    private void AddFile(string key, FakeTempLogFile file)
    {
        file.Renamed += File_Renamed;
        file.Deleted += File_Deleted;
        files.TryAdd(key, file);
    }

    private void File_Deleted(object? sender, EventArgs e)
    {
        var file = (FakeTempLogFile?)sender;
        files.TryRemove(GetLookupKey(file?.Name ?? ""), out var _);
    }

    private static string GetLookupKey(string name) => name.ToLower();

    protected override IEnumerable<string> FileNames(string pattern) =>
        files.Keys
            .Where
            (
                key => new Regex($"^{pattern.Replace("*", ".*")}$", RegexOptions.IgnoreCase).IsMatch(key)
            )
            .ToArray();
}