using ComplaintManagement.Application.Complaints;
using ComplaintManagement.Application.Dashboard;
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
        services.AddScoped<Common.Security.IRoleMappingService, Common.Security.RoleMappingService>();
        return services;
    }
}
