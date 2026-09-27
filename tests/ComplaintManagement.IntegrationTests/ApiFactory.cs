using System.Collections.Concurrent;
using System.Net.Http.Json;
using ComplaintManagement.Contracts.Requests;
using ComplaintManagement.Contracts.Responses;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace ComplaintManagement.IntegrationTests;

/// <summary>
/// Runs the API in Development (mock IAM users table + sample data) against a throwaway database on
/// the local PostgreSQL server, created for this run and dropped afterwards. The server connection comes
/// from this project's user-secrets ("ConnectionStrings:TestServer") or the CMP_TEST_POSTGRES environment
/// variable; the role needs CREATEDB. Employees sign in through the real /auth/login endpoint.
/// </summary>
public class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string DevPassword = "integration-test-password";
    public virtual int PublicLimit => 1000;
    private readonly string _serverConnection = LoadServerConnection();
    private readonly string _database = $"cmp_test_{Guid.NewGuid():N}";
    private string _connectionString = "";
    private readonly string _storage = Path.Combine(Path.GetTempPath(), $"cmp_test_files_{Guid.NewGuid():N}");
    private readonly ConcurrentDictionary<string, string> _tokens = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("ConnectionStrings:DefaultConnection", _connectionString);
        builder.UseSetting("MockIam:SeedPassword", DevPassword);
        builder.UseSetting("Auth:SigningKey", "integration-tests-signing-key-0123456789-abcdef");
        builder.UseSetting("FileStorage:BasePath", _storage);
        // Keep the background job from changing sample data while tests run.
        builder.UseSetting("Escalation:JobEnabled", "false");
        // Every test shares one client IP; the limits themselves are tested in RateLimitTests.
        builder.UseSetting("RateLimits:PublicPerMinute", PublicLimit.ToString());
        builder.UseSetting("RateLimits:LoginPerMinute", "1000");
    }

    public Task<HttpResponseMessage> LoginAsync(string employeeCode, string password) =>
        CreateClient().PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(employeeCode, password));

    /// <summary>A client signed in as the employee. Tokens are cached to stay under the login rate limit.</summary>
    public async Task<HttpClient> ClientForAsync(string employeeCode)
    {
        if (!_tokens.TryGetValue(employeeCode, out var token))
        {
            var response = await LoginAsync(employeeCode, DevPassword);
            response.EnsureSuccessStatusCode();
            token = (await response.Content.ReadFromJsonAsync<LoginResponse>())!.AccessToken;
            _tokens[employeeCode] = token;
        }
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", token);
        return client;
    }

    public async Task InitializeAsync()
    {
        await ExecuteOnServerAsync($"CREATE DATABASE \"{_database}\"");
        _connectionString = new NpgsqlConnectionStringBuilder(_serverConnection) { Database = _database }.ConnectionString;
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await base.DisposeAsync();
        NpgsqlConnection.ClearAllPools();
        await ExecuteOnServerAsync($"DROP DATABASE IF EXISTS \"{_database}\" WITH (FORCE)");
        if (Directory.Exists(_storage)) Directory.Delete(_storage, recursive: true);
    }

    private async Task ExecuteOnServerAsync(string sql)
    {
        await using var connection = new NpgsqlConnection(_serverConnection);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    private static string LoadServerConnection()
    {
        var config = new ConfigurationBuilder()
            .AddUserSecrets<ApiFactory>(optional: true)
            .AddEnvironmentVariables()
            .Build();
        return config["CMP_TEST_POSTGRES"] ?? config.GetConnectionString("TestServer")
            ?? throw new InvalidOperationException(
                "No test database server configured. Set ConnectionStrings:TestServer with 'dotnet user-secrets' in the integration test project, or CMP_TEST_POSTGRES.");
    }
}
