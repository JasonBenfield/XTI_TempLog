namespace XTI_TempLog.Extensions;

public sealed class DiskTempLogFileV1 : ITempLogFileV1
{
    private readonly string path;

    internal DiskTempLogFileV1(string path)
    {
        this.path = path;
        Name = Path.GetFileName(path);
        LastModified = new FileInfo(path).LastWriteTimeUtc;
    }

    public string Name { get; }

    public DateTimeOffset LastModified { get; }

    public ITempLogFileV1 WithNewName(string name)
    {
        var newPath = Path.Combine(Path.GetDirectoryName(path) ?? "", name);
        File.Move(path, newPath);
        return new DiskTempLogFileV1(newPath);
    }

    public void Delete() => File.Delete(path);

    public async Task<string> Read()
    {
        using var reader = new StreamReader(path);
        var content = await reader.ReadToEndAsync();
        return content;
    }

    public async Task Write(string contents)
    {
        var dir = Path.GetDirectoryName(path);
        if (dir != null && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }
        using (var writer = new StreamWriter(path))
        {
            await writer.WriteAsync(contents);
        }
    }
}
