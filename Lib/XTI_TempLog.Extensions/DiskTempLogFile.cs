using XTI_Core;
using XTI_TempLog.Abstractions;

namespace XTI_TempLog.Extensions;

public sealed class DiskTempLogFile : ITempLogFile
{
    private readonly string path;

    internal DiskTempLogFile(string path)
    {
        this.path = path;
        Name = Path.GetFileName(path);
        LastModified = new FileInfo(path).LastWriteTimeUtc;
    }

    public string Name { get; }

    public DateTimeOffset LastModified { get; }

    ITempLogFile ITempLogFile.WithNewName(string name) => WithNewName(name);

    internal DiskTempLogFile WithNewName(string name)
    {
        var newPath = Path.Combine(Path.GetDirectoryName(path) ?? "", name);
        File.Move(path, newPath);
        return new DiskTempLogFile(newPath);
    }

    public void Delete() => File.Delete(path);

    public async Task<TempLogSessionDetailModel[]> Read()
    {
        var content = await ReadFile();
        TempLogSessionDetailModel[] sessionDetails;
        if (string.IsNullOrWhiteSpace(content))
        {
            sessionDetails = [];
        }
        else
        {
            sessionDetails = XtiSerializer.DeserializeArray<TempLogSessionDetailModel>(content);
        }
        return sessionDetails;
    }

    internal async Task<string> ReadFile()
    {
        using var reader = new StreamReader(path);
        var content = await reader.ReadToEndAsync();
        return content;
    }

    public async Task Write(TempLogSessionDetailModel[] sessionDetails)
    {
        if (sessionDetails.Any())
        {
            var contents = XtiSerializer.Serialize(sessionDetails);
            await WriteFile(contents);
        }
    }

    internal async Task WriteFile(string contents)
    {
        var dir = Path.GetDirectoryName(path);
        if (dir != null && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        using var writer = new StreamWriter(path);
        await writer.WriteAsync(contents);
    }
}
