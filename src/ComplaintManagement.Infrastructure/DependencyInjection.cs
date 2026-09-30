using ComplaintManagement.Application.Common.Interfaces;
using ComplaintManagement.Application.Attachments;
using ComplaintManagement.Application.Complaints;
using ComplaintManagement.Infrastructure.FileStorage;
using ComplaintManagement.Application.Notifications;
using ComplaintManagement.Infrastructure.IAM;
using ComplaintManagement.Infrastructure.Notifications;
using ComplaintManagement.Infrastructure.IAM.Mock;
using ComplaintManagement.Infrastructure.Persistence;
using ComplaintManagement.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using StackExchange.Redis;

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
        services.AddMemoryCache();

        var storage = configuration.GetSection(FileStorageOptions.SectionName).Get<FileStorageOptions>();
        if (string.IsNullOrWhiteSpace(storage?.BasePath))
            throw new InvalidOperationException("FileStorage:BasePath is not set.");
        var storageRoot = Path.GetFullPath(Path.IsPathRooted(storage.BasePath)
            ? storage.BasePath
            : Path.Combine(environment.ContentRootPath, storage.BasePath));
        var webRoot = Path.GetFullPath(Path.Combine(environment.ContentRootPath, "wwwroot"));
        if (storageRoot.StartsWith(webRoot, StringComparison.Ordinal))
            throw new InvalidOperationException("FileStorage:BasePath must be outside the web root.");
        services.AddSingleton<IFileStorage>(new LocalFileStorage(storageRoot));
        services.AddSingleton<IMalwareScanner, NoOpMalwareScanner>();
        services.AddOptions<AttachmentOptions>().Bind(configuration.GetSection(AttachmentOptions.SectionName));

        // Distributed lock for background jobs: Redis when configured, otherwise single-instance only.
        var redis = configuration["Redis:ConnectionString"];
        if (!string.IsNullOrWhiteSpace(redis))
        {
            services.AddSingleton<IConnectionMultiplexer>(_ =>
            {
                var o = ConfigurationOptions.Parse(redis);
                o.AbortOnConnectFail = false;
                return ConnectionMultiplexer.Connect(o);
            });
            services.AddSingleton<IDistributedLock, RedisDistributedLock>();
        }
        else
        {
            if (environment.IsProduction())
                throw new InvalidOperationException("Redis:ConnectionString is not set. Background jobs need Redis locks in Production.");
            services.AddSingleton<IDistributedLock, LocalDistributedLock>();
        }
        services.AddOptions<EscalationJobOptions>().Bind(configuration.GetSection(EscalationJobOptions.SectionName));
        services.AddHostedService<EscalationJob>();

        services.AddOptions<TrackingOptions>().Bind(configuration.GetSection(TrackingOptions.SectionName));
        services.AddOptions<Application.Feedback.FeedbackOptions>()
            .Bind(configuration.GetSection(Application.Feedback.FeedbackOptions.SectionName))
            .Validate(o => string.IsNullOrWhiteSpace(o.LinkBaseUrl) || Uri.TryCreate(o.LinkBaseUrl, UriKind.Absolute, out _), "Feedback:LinkBaseUrl must be an absolute URL.")
            .ValidateOnStart();
        if (configuration.GetValue<bool>("Tracking:ExposeOtpForTesting") && environment.IsProduction())
            throw new InvalidOperationException("Tracking:ExposeOtpForTesting is on in Production. Refusing to start.");

        // Customer notifications: outbox rows are delivered by the dispatch job through the configured provider.
        var notifications = configuration.GetSection(NotificationOptions.SectionName).Get<NotificationOptions>() ?? new NotificationOptions();
        services.AddOptions<NotificationOptions>().Bind(configuration.GetSection(NotificationOptions.SectionName));
        switch (notifications.Provider)
        {
            case "Log":
                if (environment.IsProduction())
                    throw new InvalidOperationException("Notifications:Provider=Log is not allowed in Production; configure an SMS/email gateway.");
                services.AddSingleton<INotificationSender, LogNotificationSender>();
                services.AddHostedService<NotificationDispatchJob>();
                break;
            case "None":
                // Messages stay queued until a gateway is configured.
                break;
            default:
                throw new InvalidOperationException($"Unknown Notifications:Provider '{notifications.Provider}'.");
        }

        services.AddOptions<IamOptions>().Bind(configuration.GetSection(IamOptions.SectionName)).ValidateDataAnnotations().ValidateOnStart();

        var mockIam = configuration.GetSection(MockIamOptions.SectionName).Get<MockIamOptions>();
        if (mockIam?.Enabled == true)
        {
            // Temporary dummy users table until the Bank IAM login API is integrated. Never in Production.
            if (environment.IsProduction())
                throw new InvalidOperationException("MockIam is enabled in Production. Refusing to start.");
            if (string.IsNullOrEmpty(mockIam.SeedPassword) || mockIam.SeedPassword.Length < 8)
                throw new InvalidOperationException("MockIam:SeedPassword is not set (min 8 characters). Use 'dotnet user-secrets set \"MockIam:SeedPassword\" <value>'.");
            services.AddOptions<MockIamOptions>().Bind(configuration.GetSection(MockIamOptions.SectionName));
            services.AddDbContext<MockIamDbContext>(options => options.UseNpgsql(connectionString));
            services.AddScoped<IIamAuthenticator, MockIamAuthenticator>();
            services.AddScoped<IIamUserService, MockIamUserService>();
            services.AddScoped<MockIamOrganisationService>();
            services.AddScoped<IIamOrganisationService>(sp => new CachedIamOrganisationService(
                sp.GetRequiredService<MockIamOrganisationService>(), sp.GetRequiredService<IMemoryCache>()));
        }
        else
        {
            var iam = configuration.GetSection(IamOptions.SectionName).Get<IamOptions>() ?? new IamOptions();
            if (string.IsNullOrWhiteSpace(iam.BaseUrl))
                throw new InvalidOperationException("IAM:BaseUrl is not set.");
            services.AddHttpClient<IIamAuthenticator, IamAuthenticator>(client =>
            {
                client.BaseAddress = new Uri(new Uri(iam.BaseUrl.TrimEnd('/') + "/"), iam.LoginPath.TrimStart('/'));
                client.Timeout = TimeSpan.FromSeconds(iam.TimeoutSeconds);
            });
            services.AddSingleton<IIamUserService, IamUserService>();
            services.AddSingleton<IamOrganisationService>();
            services.AddSingleton<IIamOrganisationService>(sp => new CachedIamOrganisationService(
                sp.GetRequiredService<IamOrganisationService>(), sp.GetRequiredService<IMemoryCache>()));
        }

        return services;
    }
}
