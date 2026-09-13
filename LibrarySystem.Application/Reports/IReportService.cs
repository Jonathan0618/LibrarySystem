namespace LibrarySystem.Application.Reports;

public interface IReportService
{
    Task<CirculationReportDto> GetCirculationReportAsync(
        CirculationReportRequest request,
        CancellationToken cancellationToken = default);

    Task<byte[]> ExportCirculationCsvAsync(
        CirculationReportRequest request,
        CancellationToken cancellationToken = default);

    Task<OperationalReportDto> GetOperationalReportAsync(
        CirculationReportRequest request,
        CancellationToken cancellationToken = default);
}
