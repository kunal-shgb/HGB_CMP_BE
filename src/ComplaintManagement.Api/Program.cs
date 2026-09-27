using ComplaintManagement.Api;
using ComplaintManagement.Api.Authentication;
using ComplaintManagement.Api.Infrastructure;
using ComplaintManagement.Application;
using ComplaintManagement.Application.Common;
using ComplaintManagement.Infrastructure;
using ComplaintManagement.Infrastructure.IAM.Mock;
using ComplaintManagement.Infrastructure.Persistence;
using ComplaintManagement.Infrastructure.Persistence.Seed;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddOptions<SlaOptions>().Bind(builder.Configuration.GetSection(SlaOptions.SectionName)).ValidateOnStart();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration, builder.Environment);
builder.Services.AddCmpAuthentication(builder.Configuration);
builder.Services.AddCmpRateLimiting(builder.Configuration);

builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ProblemDetailsExceptionHandler>();
builder.Services.AddHealthChecks();
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    // Trust only the proxies configured for the environment (Nginx). Loopback is trusted by default.
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
});
if (builder.Environment.IsDevelopment()) builder.Services.AddOpenApi();

var app = builder.Build();

app.UseForwardedHeaders();
app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi().AllowAnonymous();

    await using (var scope = app.Services.CreateAsyncScope())
    {
        await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Database.MigrateAsync();
    }
}

// Temporary dummy users (mock IAM): create the schema and seed users. Refused in Production at startup.
if (app.Configuration.GetValue<bool>("MockIam:Enabled"))
{
    await MockIamSetup.EnsureAsync(app.Services);
    if (app.Environment.IsDevelopment()) await DevDataSeeder.SeedAsync(app.Services);
}
else
{
    app.UseHsts();
}

app.Use(async (context, next) =>
{
    context.Response.Headers.XContentTypeOptions = "nosniff";
    context.Response.Headers.XFrameOptions = "DENY";
    context.Response.Headers["Referrer-Policy"] = "no-referrer";
    context.Response.Headers.CacheControl = "no-store";
    await next();
});

app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health").AllowAnonymous();
app.MapControllers();

app.Run();

public partial class Program;
