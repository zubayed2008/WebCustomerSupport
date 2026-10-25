using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SupportDesk.Api.Security;
using SupportDesk.Application.Contracts;
using SupportDesk.Application.Services;
using SupportDesk.Domain.Permissions;

namespace SupportDesk.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/sla")]
public sealed class SlaController : ControllerBase
{
    private readonly SlaService _sla;

    public SlaController(SlaService sla)
    {
        _sla = sla;
    }

    [HttpGet("policies")]
    [HasPermission(Permissions.SlaView)]
    public async Task<ActionResult<IReadOnlyList<SlaPolicyDto>>> Policies(CancellationToken cancellationToken)
    {
        return Ok(await _sla.ListPoliciesAsync(cancellationToken));
    }

    [HttpPost("policies")]
    [HasPermission(Permissions.SlaManage)]
    public async Task<ActionResult<SlaPolicyDto>> CreatePolicy(SaveSlaPolicyRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _sla.SavePolicyAsync(null, request, cancellationToken));
    }

    [HttpPut("policies/{id:guid}")]
    [HasPermission(Permissions.SlaManage)]
    public async Task<ActionResult<SlaPolicyDto>> UpdatePolicy(Guid id, SaveSlaPolicyRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _sla.SavePolicyAsync(id, request, cancellationToken));
    }

    [HttpGet("calendars")]
    [HasPermission(Permissions.SlaView)]
    public async Task<ActionResult<IReadOnlyList<BusinessCalendarDto>>> Calendars(CancellationToken cancellationToken)
    {
        return Ok(await _sla.ListCalendarsAsync(cancellationToken));
    }

    [HttpPost("calendars")]
    [HasPermission(Permissions.SlaManage)]
    public async Task<ActionResult<BusinessCalendarDto>> CreateCalendar(SaveBusinessCalendarRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _sla.SaveCalendarAsync(null, request, cancellationToken));
    }

    [HttpPut("calendars/{id:guid}")]
    [HasPermission(Permissions.SlaManage)]
    public async Task<ActionResult<BusinessCalendarDto>> UpdateCalendar(Guid id, SaveBusinessCalendarRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _sla.SaveCalendarAsync(id, request, cancellationToken));
    }
}
