using ComplaintManagement.Application.Common.Interfaces;
using ComplaintManagement.Infrastructure.IAM;
using ComplaintManagement.Infrastructure.Persistence;
using ComplaintManagement.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ComplaintManagement.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException(
                "ConnectionStrings:DefaultConnection is not set. Use 'dotnet user-secrets' locally or an environment variable.");

        services.AddDbContext<ApplicationDbContext>(options => options.UseNpgsql(connectionString,
            npgsql => npgsql.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)));
        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());

        services.AddScoped<IComplaintNumberGenerator, ComplaintNumberGenerator>();
        services.AddScoped<IAuditLogger, AuditLogger>();

        services.AddOptions<IamOptions>().Bind(configuration.GetSection(IamOptions.SectionName)).ValidateDataAnnotations().ValidateOnStart();

        var devAuth = configuration.GetSection(DevIdentityOptions.SectionName).Get<DevIdentityOptions>();
        if (devAuth?.Enabled == true)
        {
            if (!environment.IsDevelopment())
                throw new InvalidOperationException("DevAuth is enabled outside Development. Refusing to start.");
            services.AddOptions<DevIdentityOptions>().Bind(configuration.GetSection(DevIdentityOptions.SectionName));
            services.AddSingleton<IIamUserService, DevDirectoryIamUserService>();
        }
        else
        {
            services.AddSingleton<IIamUserService, IamUserService>();
        }

        return services;
    }
}
