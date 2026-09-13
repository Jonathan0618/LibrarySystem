using LibrarySystem.Domain.Catalog;
namespace LibrarySystem.Application.Catalog;
public sealed class InventorySessionDto { public long Id{get;init;} public required string Name{get;init;} public InventorySessionStatus Status{get;init;} public DateTime StartedAtUtc{get;init;} public DateTime? CompletedAtUtc{get;init;} public int ExpectedCount{get;init;} public int FoundCount{get;init;} public int MissingCount{get;init;} public IReadOnlyCollection<InventoryCopyDto> Copies{get;init;}=[]; }
public sealed class InventoryCopyDto { public long CopyId{get;init;} public required string Barcode{get;init;} public required string Title{get;init;} public string? Shelf{get;init;} public BookCopyStatus Status{get;init;} public bool Found{get;init;} public DateTime? FoundAtUtc{get;init;} }
