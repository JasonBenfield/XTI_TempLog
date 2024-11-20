using Microsoft.Extensions.Hosting;

namespace XTI_TempLog.Extensions;

public sealed class TempLogWriterHostedService : IHostedService
{
    private readonly TempLogRepository tempLogRepo;
    private readonly TempLogOptions options;
    private readonly CancellationTokenSource cts = new();
    private Task? autoWriteTask;

    public TempLogWriterHostedService(TempLogRepository tempLogRepo, TempLogOptions options)
    {
        this.tempLogRepo = tempLogRepo;
        this.options = options;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        autoWriteTask = tempLogRepo.AutoWriteToLocalStorage
        (
            TimeSpan.FromSeconds(options.WriteIntervalInSeconds < 15 ? 15 : options.WriteIntervalInSeconds),
            cts.Token
        );
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        cts.Cancel();
        var t = autoWriteTask;
        if(t != null)
        {
            try
            {
                await t.WaitAsync(TimeSpan.FromSeconds(options.WaitForWriteInSeconds < 0 ? 15 : options.WaitForWriteInSeconds));
            }
            catch { }
        }
    }
}
