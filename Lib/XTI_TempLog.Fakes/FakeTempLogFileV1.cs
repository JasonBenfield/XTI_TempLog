namespace XTI_TempLog.Fakes;
public sealed class FakeTempLogFileV1 : ITempLogFileV1
{
    private string contents = "";
    private bool writeToConsole = false;

    internal FakeTempLogFileV1(string name, DateTimeOffset lastModified)
    {
        Name = name;
        LastModified = lastModified;
    }

    public void WriteToConsole()
    {
        writeToConsole = true;
    }

    public string Name { get; }
    public DateTimeOffset LastModified { get; }

    public event EventHandler<RenamedEventArgsV1>? Renamed;

    public ITempLogFileV1 WithNewName(string name)
    {
        var newFile = new FakeTempLogFileV1(name, LastModified) { contents = contents };
        Renamed?.Invoke(this, new RenamedEventArgsV1(this, newFile));
        return newFile;
    }

    public Task<string> Read() => Task.FromResult(contents);

    public Task Write(string contents)
    {
        if (writeToConsole)
        {
            Console.WriteLine($"Temp Log {Name}\r\n{contents}");
        }
        this.contents = contents;
        return Task.CompletedTask;
    }

    public event EventHandler? Deleted;

    public void Delete() => Deleted?.Invoke(this, new EventArgs());

}