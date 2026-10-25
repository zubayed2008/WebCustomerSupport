using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SupportDesk.Api.Security;
using SupportDesk.Application.Abstractions;
using SupportDesk.Application.Contracts;
using SupportDesk.Domain.Permissions;

namespace SupportDesk.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/lookups")]
public sealed class LookupsController : ControllerBase
{
    private readonly IAppDbContext _dbContext;
    private readonly IUserDirectory _users;

    public LookupsController(IAppDbContext dbContext, IUserDirectory users)
    {
        _dbContext = dbContext;
        _users = users;
    }

    [HttpGet("categories")]
    public async Task<ActionResult<IReadOnlyList<CategoryDto>>> Categories(CancellationToken cancellationToken)
    {
        var categories = await _dbContext.Categories
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.SortOrder).ThenBy(x => x.Name)
            .Select(x => new CategoryDto(x.Id, x.Name, x.Description, x.SortOrder, x.IsActive))
            .ToListAsync(cancellationToken);

        return Ok(categories);
    }

    [HttpGet("assignees")]
    [HasPermission(Permissions.TicketsAssign)]
    public async Task<ActionResult<IReadOnlyList<AssigneeDto>>> Assignees(CancellationToken cancellationToken)
    {
        return Ok(await _users.GetSupportUsersAsync(cancellationToken));
    }

    [HttpGet("tags")]
    public async Task<ActionResult<IReadOnlyList<string>>> Tags(CancellationToken cancellationToken)
    {
        var tags = await _dbContext.Tags.AsNoTracking().OrderBy(x => x.Name).Select(x => x.Name).Take(200).ToListAsync(cancellationToken);
        return Ok(tags);
    }
}
