namespace LibrarySystem.Application.Reports;

public sealed class AuditLogDto
{
    public long Id { get; init; }
    public string? ActorUserId { get; init; }
    public required string ActorName { get; init; }
    public required string Action { get; init; }
    public required string TargetType { get; init; }
    public required string TargetId { get; init; }
    public string? Details { get; init; }
    public DateTime CreatedAtUtc { get; init; }
}
