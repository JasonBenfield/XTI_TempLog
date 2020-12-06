using Microsoft.AspNetCore.DataProtection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace XTI_TempLog.Extensions
{
    public sealed class DiskTempLogs : TempLogs
    {
        private readonly IDataProtector dataProtector;
        private readonly string topLevelPath;
        private readonly string directoryName;

        public DiskTempLogs(IDataProtector dataProtector, string topLevelPath, string directoryName)
        {
            this.dataProtector = dataProtector;
            this.topLevelPath = topLevelPath;
            this.directoryName = directoryName;
        }

        public IEnumerable<TempLog> Logs()
        {
            var paths = findTempLogPaths(topLevelPath);
            return paths.Select(path => new DiskTempLog(dataProtector, path));
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
}
