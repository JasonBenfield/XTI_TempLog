using XTI_Core;
using XTI_TempLog.Abstractions;

namespace XTI_TempLog.Fakes;

public sealed class FakeTempLogFile : ITempLogFile
{
    private string contents = "";

    internal FakeTempLogFile(string name, DateTimeOffset lastModified)
    {
        Name = name;
        LastModified = lastModified;
    }

    public string Name { get;  }

    public DateTimeOffset LastModified { get; }

    public event EventHandler<RenamedEventArgs>? Renamed;

    public event EventHandler? Deleted;

    public void Delete() => Deleted?.Invoke(this, new EventArgs());

    public Task<TempLogSessionDetailModel[]> Read()
    {
        var sessionDetails = string.IsNullOrWhiteSpace(contents) ?
            [] :
            XtiSerializer.DeserializeArray<TempLogSessionDetailModel>(contents);
        return Task.FromResult(sessionDetails);
    }

    public ITempLogFile WithNewName(string name)
    {
        var newFile = new FakeTempLogFile(name, LastModified) { contents = contents };
        Renamed?.Invoke(this, new RenamedEventArgs(this, newFile));
        return newFile;
    }

    public Task Write(TempLogSessionDetailModel[] sessionDetails)
    {
        contents = XtiSerializer.Serialize(sessionDetails);
        return Task.CompletedTask;
    }
}
