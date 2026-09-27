using ComplaintManagement.Application.Common.Security;
using ComplaintManagement.Application.Employees;
using ComplaintManagement.Contracts.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ComplaintManagement.Api.Controllers;

/// <summary>Active employees the caller may assign complaints to, limited to their office.</summary>
[ApiController]
[Route("api/v1/employees")]
[Authorize(Policy = Permissions.ComplaintAssign)]
public sealed class EmployeesController(IEmployeeDirectoryService directory) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<EmployeeResponse>> Search([FromQuery] string? q, CancellationToken ct) =>
        directory.SearchAssignableAsync(q, ct);
}
