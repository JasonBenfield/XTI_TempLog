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
                var childDirInfo = new DirectoryInfo(childDirectoryPath);
                if (childDirInfo.Name.Equals(directoryName, StringComparison.OrdinalIgnoreCase))
                {
                    if (!parentDirectoryPath.Equals(topLevelPath, StringComparison.OrdinalIgnoreCase))
                    {
                        tempLogDirectories.Add(childDirectoryPath);
                    }
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