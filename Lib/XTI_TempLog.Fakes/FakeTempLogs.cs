using System.Collections.Generic;

namespace XTI_TempLog.Fakes
{
    public sealed class FakeTempLogs : TempLogs
    {
        private readonly List<TempLog> tempLogs = new List<TempLog>();

        public FakeTempLogs(TempLog tempLog)
        {
            if (tempLog != null)
            {
                Add(tempLog);
            }
        }

        public IEnumerable<TempLog> Logs() => tempLogs;

        public void Add(TempLog tempLog) => tempLogs.Add(tempLog);
    }
}
