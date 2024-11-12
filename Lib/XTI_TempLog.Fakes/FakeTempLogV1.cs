using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using XTI_Core;

namespace XTI_TempLog.Fakes;

public sealed class FakeTempLogV1 : TempLogV1
{
    private readonly ConcurrentDictionary<string, FakeTempLogFileV1> files = new();
    private readonly IClock clock;
    private bool writeToConsole;

    public FakeTempLogV1(IClock clock)
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

    protected override ITempLogFileV1 CreateFile(string name)
    {
        var key = getLookupKey(name);
        if (!files.TryGetValue(key, out var file))
        {
            var lastModified = clock.Now();
            file = new FakeTempLogFileV1(name, lastModified);
            if (writeToConsole)
            {
                file.WriteToConsole();
            }
            addFile(key, file);
        }
        return file;
    }

    private void File_Renamed(object? sender, RenamedEventArgsV1 e)
    {
        files.TryRemove(getLookupKey(e.OldFile.Name), out var _);
        var key = getLookupKey(e.NewFile.Name);
        addFile(key, e.NewFile);
    }

    private void addFile(string key, FakeTempLogFileV1 file)
    {
        file.Renamed += File_Renamed;
        file.Deleted += File_Deleted;
        files.TryAdd(key, file);
    }

    private void File_Deleted(object? sender, EventArgs e)
    {
        var file = (FakeTempLogFileV1?)sender;
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