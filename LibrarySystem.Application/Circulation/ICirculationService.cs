namespace LibrarySystem.Application.Circulation;

using LibrarySystem.Application.Common;

public interface ICirculationService
{
    Task<IReadOnlyCollection<LoanDto>> CheckoutAsync(
        CheckoutRequest request,
        CancellationToken cancellationToken = default);

    Task<LoanDto?> GetLoanAsync(long id, CancellationToken cancellationToken = default);

    Task<PagedResult<LoanDto>> SearchAsync(
        LoanSearchRequest request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<LoanDto>> GetActiveLoansForMemberAsync(
        long memberId,
        CancellationToken cancellationToken = default);

    Task<LoanDto?> ReturnAsync(
        long loanId,
        ReturnLoanRequest request,
        CancellationToken cancellationToken = default);

    Task<LoanDto?> RenewAsync(
        long loanId,
        RenewLoanRequest request,
        CancellationToken cancellationToken = default);

    Task<LoanDto?> MarkLostAsync(
        long loanId,
        MarkLoanLostRequest request,
        CancellationToken cancellationToken = default);
}
