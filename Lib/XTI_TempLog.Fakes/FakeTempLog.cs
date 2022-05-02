using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using XTI_Core;

namespace XTI_TempLog.Fakes;

public sealed class FakeTempLog : TempLog
{
    private readonly ConcurrentDictionary<string, FakeTempLogFile> files = new();
    private readonly IClock clock;
    private bool writeToConsole;

    public FakeTempLog(IClock clock)
    {
        this.clock = clock;
    }

    public void WriteToConsole()
    {
        writeToConsole = true;
        foreach (var file in files.Values)
        {
            file.WriteToConsole();
        }
    }

    public string[] Files() => files.Keys.ToArray();

    protected override ITempLogFile CreateFile(string name)
    {
        var key = getLookupKey(name);
        if (!files.TryGetValue(key, out var file))
        {
            var lastModified = clock.Now();
            file = new FakeTempLogFile(name, lastModified);
            if (writeToConsole)
            {
                file.WriteToConsole();
            }
            addFile(key, file);
        }
        return file;
    }

    private void File_Renamed(object? sender, RenamedEventArgs e)
    {
        files.TryRemove(getLookupKey(e.OldFile.Name), out var _);
        var key = getLookupKey(e.NewFile.Name);
        addFile(key, e.NewFile);
    }

    private void addFile(string key, FakeTempLogFile file)
    {
        file.Renamed += File_Renamed;
        file.Deleted += File_Deleted;
        files.TryAdd(key, file);
    }

    private void File_Deleted(object? sender, EventArgs e)
    {
        var file = (FakeTempLogFile?)sender;
        files.TryRemove(getLookupKey(file?.Name ?? ""), out var _);
    }

    private static string getLookupKey(string name) => name.ToLower();

    protected override IEnumerable<string> FileNames(string pattern)
        => files.Keys.Where
        (
            key => new Regex($"^{pattern.Replace("*", ".*")}$", RegexOptions.IgnoreCase).IsMatch(key)
        )
        .ToArray();
}