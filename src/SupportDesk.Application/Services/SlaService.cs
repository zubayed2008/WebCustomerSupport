using Microsoft.EntityFrameworkCore;
using SupportDesk.Application.Abstractions;
using SupportDesk.Application.Contracts;
using SupportDesk.Domain;
using SupportDesk.Domain.Common;
using SupportDesk.Domain.Permissions;
using SupportDesk.Domain.Sla;

namespace SupportDesk.Application.Services;

public sealed partial class SlaService
{
    private readonly IAppDbContext _dbContext;
    private readonly IClock _clock;
    private readonly ICurrentUser _currentUser;
    private readonly IAuditWriter _audit;

    public SlaService(IAppDbContext dbContext, IClock clock, ICurrentUser currentUser, IAuditWriter audit)
    {
        _dbContext = dbContext;
        _clock = clock;
        _currentUser = currentUser;
        _audit = audit;
    }

    public async Task<IReadOnlyList<SlaPolicyDto>> ListPoliciesAsync(CancellationToken cancellationToken)
    {
        RequirePermission(Permissions.SlaView);
        var policies = await _dbContext.SlaPolicies
            .AsNoTracking()
            .Include(x => x.BusinessCalendar)
            .OrderBy(x => x.Priority)
            .ToListAsync(cancellationToken);

        return policies.Select(MapPolicy).ToList();
    }

    public async Task<SlaPolicyDto> SavePolicyAsync(Guid? id, SaveSlaPolicyRequest request, CancellationToken cancellationToken)
    {
        var userId = RequireAuthenticated();
        RequirePermission(Permissions.SlaManage);

        if (!Enum.TryParse<TicketPriority>(request.Priority, true, out var priority))
        {
            throw new ValidationFailedException("Priority is not valid.");
        }

        var calendar = await _dbContext.BusinessCalendars.FirstOrDefaultAsync(x => x.Id == request.BusinessCalendarId, cancellationToken)
            ?? throw new ValidationFailedException("Business calendar not found.");

        var now = _clock.UtcNow;
        SlaPolicy policy;
        if (id is null)
        {
            policy = new SlaPolicy { CreatedAt = now, UpdatedAt = now };
            _dbContext.SlaPolicies.Add(policy);
        }
        else
        {
            policy = await _dbContext.SlaPolicies.FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
                ?? throw new NotFoundException("SLA policy not found.");
        }

        if (request.IsActive)
        {
            var otherActive = await _dbContext.SlaPolicies
                .Where(x => x.Id != policy.Id && x.Priority == priority && x.IsActive)
                .ToListAsync(cancellationToken);
            foreach (var other in otherActive)
            {
                other.IsActive = false;
                other.UpdatedAt = now;
            }
        }

        policy.Name = request.Name.Trim();
        policy.Priority = priority;
        policy.FirstResponseMinutes = request.FirstResponseMinutes;
        policy.ResolutionMinutes = request.ResolutionMinutes;
        policy.DueSoonMinutes = request.DueSoonMinutes;
        policy.BusinessCalendarId = request.BusinessCalendarId;
        policy.BusinessCalendar = calendar;
        policy.IsActive = request.IsActive;
        policy.UpdatedAt = now;
        if (id is not null)
        {
            policy.Version++;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        await _audit.WriteAsync(userId, id is null ? "sla.policy.create" : "sla.policy.update", nameof(SlaPolicy), policy.Id.ToString(), new { policy.Name, request.Priority }, cancellationToken);
        return MapPolicy(policy);
    }

    private Guid RequireAuthenticated()
    {
        return _currentUser.UserId ?? throw new ForbiddenException("Authentication is required.");
    }

    private void RequirePermission(string permission)
    {
        if (!_currentUser.HasPermission(permission))
        {
            throw new ForbiddenException($"Missing permission '{permission}'.");
        }
    }

    private static SlaPolicyDto MapPolicy(SlaPolicy policy)
    {
        return new SlaPolicyDto(
            policy.Id,
            policy.Name,
            policy.Priority.ToString(),
            policy.FirstResponseMinutes,
            policy.ResolutionMinutes,
            policy.DueSoonMinutes,
            policy.Version,
            policy.IsActive,
            policy.BusinessCalendarId,
            policy.BusinessCalendar?.Name ?? string.Empty);
    }
}
