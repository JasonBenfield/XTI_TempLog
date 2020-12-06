using System.Collections.Generic;

namespace XTI_TempLog
{
    public interface TempLogs
    {
        IEnumerable<TempLog> Logs();
    }
}
