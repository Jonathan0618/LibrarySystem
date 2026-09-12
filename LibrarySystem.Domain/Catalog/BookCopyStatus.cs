namespace LibrarySystem.Domain.Catalog;

public enum BookCopyStatus
{
    Available = 1,
    OnLoan = 2,
    Reserved = 3,
    Lost = 4,
    Damaged = 5,
    Withdrawn = 6
}
