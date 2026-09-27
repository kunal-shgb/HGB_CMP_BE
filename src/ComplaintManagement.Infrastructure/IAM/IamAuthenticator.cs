using System.Net;
using System.Net.Http.Json;
using ComplaintManagement.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace ComplaintManagement.Infrastructure.IAM;

/// <summary>
/// Calls the Bank IAM login API with the employee code and password and reads back the user profile.
/// The request body and success response follow the profile shape the Bank shared; confirm field names
/// and the endpoint path against the IAM API specification. The password is never logged.
/// </summary>
internal sealed class IamAuthenticator(HttpClient http, ILogger<IamAuthenticator> logger) : IIamAuthenticator
{
    public async Task<IamUser?> AuthenticateAsync(string employeeCode, string password, CancellationToken cancellationToken = default)
    {
        HttpResponseMessage response;
        try
        {
            response = await http.PostAsJsonAsync("", new { employeeCode, password }, cancellationToken);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            logger.LogError("Bank IAM login request failed: {Error}", ex.GetType().Name);
            throw new IamUnavailableException("The Bank sign-in service could not be reached.", ex);
        }

        using (response)
        {
            if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.BadRequest or HttpStatusCode.NotFound or HttpStatusCode.Forbidden)
                return null;
            if (!response.IsSuccessStatusCode)
            {
                logger.LogError("Bank IAM login returned {StatusCode}", (int)response.StatusCode);
                throw new IamUnavailableException("The Bank sign-in service returned an error.");
            }

            IamUserProfile? profile;
            try
            {
                profile = await response.Content.ReadFromJsonAsync<IamUserProfile>(cancellationToken);
            }
            catch (Exception ex) when (ex is System.Text.Json.JsonException or NotSupportedException)
            {
                logger.LogError("Bank IAM login returned an unreadable profile");
                throw new IamUnavailableException("The Bank sign-in service returned an unexpected response.", ex);
            }

            if (profile is null || string.IsNullOrWhiteSpace(profile.EmployeeCode))
                throw new IamUnavailableException("The Bank sign-in service returned an empty profile.");
            return IamProfileMapper.ToIamUser(profile);
        }
    }
}
