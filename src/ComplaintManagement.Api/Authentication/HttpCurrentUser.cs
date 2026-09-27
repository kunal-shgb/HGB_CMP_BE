using System.Security.Claims;
using ComplaintManagement.Application.Common.Interfaces;
using ComplaintManagement.Application.Common.Security;
using ComplaintManagement.Domain.Enums;
using Microsoft.Extensions.Options;

namespace ComplaintManagement.Api.Authentication;

public sealed class HttpCurrentUser(IHttpContextAccessor accessor, IOptions<OfficeScopeOptions> officeScopes) : ICurrentUser
{
    private ClaimsPrincipal Principal =>
        accessor.HttpContext?.User is { Identity.IsAuthenticated: true } user
            ? user
            : throw new InvalidOperationException("No authenticated employee in the current request.");

    private IReadOnlySet<string>? _roles;
    private IReadOnlySet<string>? _permissions;

    public string EmployeeId => Principal.FindFirstValue(CmpClaimTypes.EmployeeId)
        ?? throw new InvalidOperationException("Authenticated principal has no employee code claim.");
    public string Name => Principal.FindFirstValue(CmpClaimTypes.Name) ?? EmployeeId;
    public string? Designation => Principal.FindFirstValue(CmpClaimTypes.Designation);
    public IReadOnlySet<string> Roles => _roles ??= Principal.FindAll(CmpClaimTypes.AppRole).Select(c => c.Value).ToHashSet();
    public ScopeLevel? ScopeLevel => officeScopes.Value.Resolve(OfficeType);
    public string? OfficeType => Principal.FindFirstValue(CmpClaimTypes.OfficeType);
    public string? OfficeCode => Principal.FindFirstValue(CmpClaimTypes.OfficeCode);
    public string? OfficeName => Principal.FindFirstValue(CmpClaimTypes.OfficeName);
    public string? DepartmentName => Principal.FindFirstValue(CmpClaimTypes.DepartmentName);
    public string? IpAddress => accessor.HttpContext?.Connection.RemoteIpAddress?.ToString();
    public string? UserAgent => accessor.HttpContext?.Request.Headers.UserAgent.ToString();

    public IReadOnlySet<string> Permissions => _permissions ??= Application.Common.Security.Permissions.ForRoles(Roles);
    public bool HasPermission(string permission) => Permissions.Contains(permission);
}
