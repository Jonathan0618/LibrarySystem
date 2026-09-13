namespace LibrarySystem.Application.Fines;

public interface IOverdueProcessor
{
    Task ProcessAsync(CancellationToken cancellationToken = default);
    Task DeliverNotificationsAsync(CancellationToken cancellationToken = default);
}
