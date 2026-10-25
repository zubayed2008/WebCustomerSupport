using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SupportDesk.Application.Contracts;
using SupportDesk.Application.Services;

namespace SupportDesk.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/reports")]
public sealed class ReportsController : ControllerBase
{
    private readonly ReportService _reports;

    public ReportsController(ReportService reports)
    {
        _reports = reports;
    }

    [HttpGet("overview")]
    public async Task<ActionResult<ReportOverviewDto>> Overview(CancellationToken cancellationToken)
    {
        return Ok(await _reports.GetOverviewAsync(cancellationToken));
    }
}
