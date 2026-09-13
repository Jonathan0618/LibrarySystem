namespace LibrarySystem.Domain.Circulation;

public enum LoanHistoryAction
{
    CheckedOut = 1,
    Renewed = 2,
    Returned = 3,
    MarkedOverdue = 4,
    MarkedLost = 5
}
