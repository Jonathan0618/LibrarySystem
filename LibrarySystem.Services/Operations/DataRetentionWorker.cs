using LibrarySystem.Application.Operations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LibrarySystem.Services.Operations;

public sealed partial class DataRetentionWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<DataRetentionOptions> options,
    TimeProvider timeProvider,
    ILogger<DataRetentionWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.Enabled)
        {
            return;
        }

        using var timer = new PeriodicTimer(
            TimeSpan.FromHours(options.Value.IntervalHours),
            timeProvider);
        do
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<IDataRetentionService>();
                DataRetentionResult result;
                do
                {
                    result = await service.ApplyAsync(stoppingToken);
                    LogRetentionResult(logger, result.DeletedAuditLogCount, result.DeletedTerminalNotificationCount);
                }
                while (result.MoreRecordsRemain && !stoppingToken.IsCancellationRequested);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                LogRetentionFailure(logger, exception);
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    [LoggerMessage(LogLevel.Information, "Data retention deleted {AuditCount} audit logs and {NotificationCount} terminal notifications.")]
    private static partial void LogRetentionResult(ILogger logger, int auditCount, int notificationCount);

    [LoggerMessage(LogLevel.Error, "Data retention execution failed.")]
    private static partial void LogRetentionFailure(ILogger logger, Exception exception);
}
