using LibrarySystem.Application.Catalog;
using LibrarySystem.Application.Circulation;
using LibrarySystem.Application.Dashboard;
using LibrarySystem.Application.Members;
using LibrarySystem.Application.Fines;
using LibrarySystem.Application.Identity;
using LibrarySystem.Application.Notifications;
using LibrarySystem.Application.Operations;
using LibrarySystem.Application.Reports;
using LibrarySystem.Application.Reservations;
using LibrarySystem.Services.Catalog;
using LibrarySystem.Services.Circulation;
using LibrarySystem.Services.Dashboard;
using LibrarySystem.Services.Members;
using LibrarySystem.Services.Fines;
using LibrarySystem.Services.Identity;
using LibrarySystem.Services.Notifications;
using LibrarySystem.Services.Operations;
using LibrarySystem.Services.Reports;
using LibrarySystem.Services.Reservations;
using Microsoft.Extensions.DependencyInjection;

namespace LibrarySystem.Services;

public static class DependencyInjection
{
    public static IServiceCollection AddLibraryServices(this IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<ICatalogReferenceService, CatalogReferenceService>();
        services.AddScoped<ICatalogService, CatalogService>();
        services.AddScoped<IInventoryService, InventoryService>();
        services.AddScoped<IAcquisitionService, AcquisitionService>();
        services.AddScoped<ReservationService>();
        services.AddScoped<IReservationService>(serviceProvider =>
        serviceProvider.GetRequiredService<ReservationService>());
        services.AddHostedService<ReservationExpiryWorker>();
        services.AddScoped<ICirculationService, CirculationService>();
        services.AddScoped<IDueDateCalculator, DueDateCalculator>();
        services.AddScoped<ISchoolCalendarService, SchoolCalendarService>();
        services.AddScoped<ILibrarianDashboardService, LibrarianDashboardService>();
        services.AddScoped<IStudentDashboardService, StudentDashboardService>();
        services.AddScoped<IMemberService, MemberService>();
        services.AddScoped<IRoleManagementService, RoleManagementService>();
        services.AddScoped<IReportService, ReportService>();
        services.AddScoped<IAuditLogService, AuditLogService>();
        services.AddScoped<IFineService, FineService>();
        services.AddScoped<IOverdueProcessor, OverdueProcessor>();
        services.AddScoped<IAccountNotificationService, AccountNotificationService>();
        services.AddScoped<INotificationOperationsService, NotificationOperationsService>();
        services.AddScoped<IIdentityRecoveryService, IdentityRecoveryService>();
        services.AddHostedService<OverdueProcessingWorker>();
        services.AddScoped<IDataRetentionService, DataRetentionService>();
        services.AddHostedService<DataRetentionWorker>();
        return services;
    }
}
