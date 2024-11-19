using Microsoft.AspNetCore.DataProtection;

namespace XTI_TempLog.Extensions;

public sealed class DiskTempLogsV1 : ITempLogsV1
{
    private readonly IDataProtector dataProtector;
    private readonly string topLevelPath;
    private readonly string directoryName;

    public DiskTempLogsV1(IDataProtector dataProtector, string topLevelPath, string directoryName)
    {
        this.dataProtector = dataProtector;
        this.topLevelPath = topLevelPath;
        this.directoryName = directoryName;
    }

    public IEnumerable<TempLogV1> Logs()
    {
        var paths = findTempLogPaths(topLevelPath);
        return paths.Select(path => new DiskTempLogV1(dataProtector, path));
    }

    private IEnumerable<string> findTempLogPaths(string parentDirectoryPath)
    {
        var tempLogDirectories = new List<string>();
        if (Directory.Exists(parentDirectoryPath))
        {
            var childDirectoryPaths = Directory.GetDirectories(parentDirectoryPath);
            foreach (var childDirectoryPath in childDirectoryPaths)
            {
                if (new DirectoryInfo(childDirectoryPath).Name.Equals(directoryName, StringComparison.OrdinalIgnoreCase))
                {
                    tempLogDirectories.Add(childDirectoryPath);
                }
                else
                {
                    var childTempLogPaths = findTempLogPaths(childDirectoryPath);
                    tempLogDirectories.AddRange(childTempLogPaths);
                }
            }
        }
        return tempLogDirectories;
    }
}