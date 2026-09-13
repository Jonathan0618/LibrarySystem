using LibrarySystem.Application.Reservations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LibrarySystem.Services.Reservations;

public sealed partial class ReservationExpiryWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<ReservationExpiryWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromHours(1));
        do
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var reservationService = scope.ServiceProvider.GetRequiredService<IReservationService>();
                var expiredCount = await reservationService.ExpireReadyReservationsAsync(stoppingToken);
                if (expiredCount > 0)
                {
                    LogReservationsExpired(logger, expiredCount);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                LogExpirationFailure(logger, exception);
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    [LoggerMessage(EventId = 10, Level = LogLevel.Information, Message = "Expired {Count} ready reservations.")]
    private static partial void LogReservationsExpired(ILogger logger, int count);

    [LoggerMessage(EventId = 11, Level = LogLevel.Error, Message = "Reservation expiry processing failed.")]
    private static partial void LogExpirationFailure(ILogger logger, Exception exception);
}
