using ComplaintManagement.Application.Admin;
using ComplaintManagement.Application.Attachments;
using ComplaintManagement.Application.Auth;
using ComplaintManagement.Application.Complaints;
using ComplaintManagement.Application.Dashboard;
using ComplaintManagement.Application.Employees;
using ComplaintManagement.Application.Escalation;
using ComplaintManagement.Application.Notifications;
using ComplaintManagement.Application.Reference;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace ComplaintManagement.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly, includeInternalTypes: true);
        services.AddScoped<IComplaintService, ComplaintService>();
        services.AddScoped<IPublicComplaintService, PublicComplaintService>();
        services.AddScoped<IReferenceService, ReferenceService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IEmployeeDirectoryService, EmployeeDirectoryService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IAdminService, AdminService>();
        services.AddScoped<AttachmentStore>();
        services.AddScoped<IAttachmentService, AttachmentService>();
        services.AddScoped<IEscalationService, EscalationService>();
        services.AddScoped<CustomerNotifier>();
        services.AddScoped<ITrackingService, TrackingService>();
        services.AddScoped<INotificationDispatcher, NotificationDispatcher>();
        services.AddScoped<Common.Security.IRoleMappingService, Common.Security.RoleMappingService>();
        return services;
    }
}
