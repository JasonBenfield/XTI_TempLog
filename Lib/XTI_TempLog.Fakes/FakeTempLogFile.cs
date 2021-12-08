namespace XTI_TempLog.Fakes;
public sealed class FakeTempLogFile : ITempLogFile
{
    private string contents = "";
    private bool writeToConsole = false;

    internal FakeTempLogFile(string name, DateTimeOffset lastModified)
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

    public event EventHandler<RenamedEventArgs>? Renamed;

    public ITempLogFile WithNewName(string name)
    {
        var newFile = new FakeTempLogFile(name, LastModified) { contents = contents };
        Renamed?.Invoke(this, new RenamedEventArgs(this, newFile));
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