namespace LibrarySystem.Application.Operations;

public interface IDataRetentionService
{
    Task<DataRetentionPreview> PreviewAsync(CancellationToken cancellationToken = default);

    Task<DataRetentionResult> ApplyAsync(CancellationToken cancellationToken = default);
}
