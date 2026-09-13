using LibrarySystem.Application.Fines;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LibrarySystem.Services.Fines;

public sealed partial class OverdueProcessingWorker(IServiceScopeFactory scopeFactory, ILogger<OverdueProcessingWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromHours(1));
        do
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var processor = scope.ServiceProvider.GetRequiredService<IOverdueProcessor>();
                await processor.ProcessAsync(stoppingToken);
                await processor.DeliverNotificationsAsync(stoppingToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                LogProcessingFailed(logger, exception);
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    [LoggerMessage(3, LogLevel.Error, "Overdue processing failed and will be retried.")]
    private static partial void LogProcessingFailed(ILogger logger, Exception exception);
}
