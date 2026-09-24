using ComplaintManagement.Application.Common.Interfaces;
using ComplaintManagement.Application.Common.Security;
using ComplaintManagement.Contracts.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ComplaintManagement.Api.Controllers;

/// <summary>Assignable employees, looked up in the Bank directory and limited to the caller's scope.</summary>
[ApiController]
[Route("api/v1/employees")]
[Authorize(Policy = Permissions.ComplaintAssign)]
public sealed class EmployeesController(IIamUserService iam, ICurrentUser user) : ControllerBase
{
    [HttpGet]
    public async Task<IReadOnlyList<EmployeeResponse>> Search([FromQuery] string? q, CancellationToken ct)
    {
        var users = await iam.SearchUsersAsync(q?.Trim(), ct);
        return users
            .Where(u => ComplaintScope.CanTarget(user, u))
            .OrderBy(u => u.Name)
            .Take(50)
            .Select(u => new EmployeeResponse(u.EmployeeId, u.Name, u.Designation, u.BranchCode, u.RegionCode, u.DepartmentCode))
            .ToList();
    }
}
