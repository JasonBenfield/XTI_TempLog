namespace XTI_TempLog.Fakes
{
    public sealed class FakeTempLogsV1 : ITempLogsV1
    {
        private readonly List<TempLogV1> tempLogs = new();

        public FakeTempLogsV1(TempLogV1 tempLog)
        {
            if (tempLog != null)
            {
                Add(tempLog);
            }
        }

        public IEnumerable<TempLogV1> Logs() => tempLogs;

        public void Add(TempLogV1 tempLog) => tempLogs.Add(tempLog);
    }
}
