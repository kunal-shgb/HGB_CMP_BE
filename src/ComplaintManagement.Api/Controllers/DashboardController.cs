using ComplaintManagement.Application.Common.Security;
using ComplaintManagement.Application.Dashboard;
using ComplaintManagement.Contracts.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ComplaintManagement.Api.Controllers;

[ApiController]
[Route("api/v1/dashboard")]
[Authorize(Policy = Permissions.DashboardView)]
public sealed class DashboardController(IDashboardService dashboard) : ControllerBase
{
    [HttpGet("summary")]
    public Task<DashboardSummary> Summary([FromQuery] int days = 30, CancellationToken ct = default) =>
        dashboard.GetSummaryAsync(days, ct);
}
