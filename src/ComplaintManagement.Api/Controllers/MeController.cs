using ComplaintManagement.Api.Authentication;
using ComplaintManagement.Application.Common.Interfaces;
using ComplaintManagement.Contracts.Responses;
using Microsoft.AspNetCore.Mvc;

namespace ComplaintManagement.Api.Controllers;

/// <summary>The signed-in employee, their application roles, permissions and office scope. The portal uses this to shape its UI.</summary>
[ApiController]
[Route("api/v1/me")]
public sealed class MeController(ICurrentUser user) : ControllerBase
{
    [HttpGet]
    public ActionResult<CurrentUserResponse> Get() => new CurrentUserResponse(
        user.EmployeeId, user.Name, user.Designation,
        user.Roles.OrderBy(r => r).ToList(),
        ((HttpCurrentUser)user).Permissions.OrderBy(p => p).ToList(),
        user.ScopeLevel?.ToString(), user.OfficeType, user.OfficeCode, user.OfficeName, user.DepartmentName);
}
