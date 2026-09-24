using System.Security.Claims;
using ComplaintManagement.Application.Common.Interfaces;
using ComplaintManagement.Application.Common.Security;
using ComplaintManagement.Domain.Enums;

namespace ComplaintManagement.Api.Authentication;

public sealed class HttpCurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    private ClaimsPrincipal Principal =>
        accessor.HttpContext?.User is { Identity.IsAuthenticated: true } user
            ? user
            : throw new InvalidOperationException("No authenticated employee in the current request.");

    private IReadOnlySet<string>? _roles;
    private IReadOnlySet<string>? _permissions;

    public string EmployeeId => Principal.FindFirstValue(CmpClaimTypes.EmployeeId)
        ?? throw new InvalidOperationException("Authenticated principal has no employee ID claim.");
    public string Name => Principal.FindFirstValue(CmpClaimTypes.Name) ?? EmployeeId;
    public string? Designation => Principal.FindFirstValue(CmpClaimTypes.Designation);
    public IReadOnlySet<string> Roles => _roles ??= Principal.FindAll(CmpClaimTypes.AppRole).Select(c => c.Value).ToHashSet();
    public ScopeLevel ScopeLevel => AppRoles.ResolveScope(Roles);
    public string? RegionCode => Principal.FindFirstValue(CmpClaimTypes.Region);
    public string? BranchCode => Principal.FindFirstValue(CmpClaimTypes.Branch);
    public string? DepartmentCode => Principal.FindFirstValue(CmpClaimTypes.Department);
    public string? IpAddress => accessor.HttpContext?.Connection.RemoteIpAddress?.ToString();
    public string? UserAgent => accessor.HttpContext?.Request.Headers.UserAgent.ToString();

    public IReadOnlySet<string> Permissions => _permissions ??= Application.Common.Security.Permissions.ForRoles(Roles);
    public bool HasPermission(string permission) => Permissions.Contains(permission);
}
