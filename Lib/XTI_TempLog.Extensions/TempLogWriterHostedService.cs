using Microsoft.Extensions.Hosting;

namespace XTI_TempLog.Extensions;

public sealed class TempLogWriterHostedService : IHostedService
{
    private readonly TempLogRepository tempLogRepo;
    private readonly TempLogOptions options;
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
            cancellationToken
        );
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        var t = autoWriteTask;
        if(t != null)
        {
            await t.WaitAsync(TimeSpan.FromSeconds(5));
        }
    }
}
