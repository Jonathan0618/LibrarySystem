using LibrarySystem.Domain.Catalog;
namespace LibrarySystem.Application.Catalog;
public sealed class AcquisitionDto { public long Id{get;init;} public required string OrderNumber{get;init;} public long BookId{get;init;} public required string BookTitle{get;init;} public AcquisitionSource Source{get;init;} public string? DonationSource{get;init;} public int QuantityOrdered{get;init;} public int QuantityReceived{get;init;} public decimal UnitCost{get;init;} public AcquisitionStatus Status{get;init;} public DateTime CreatedAtUtc{get;init;} public required byte[] RowVersion{get;init;} }
public sealed class AcquisitionBudgetDto { public int Year{get;init;} public decimal Allocated{get;init;} public decimal Spent{get;init;} public decimal Remaining=>Allocated-Spent; }
