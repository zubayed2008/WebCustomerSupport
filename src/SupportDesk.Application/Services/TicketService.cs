using Microsoft.EntityFrameworkCore;
using SupportDesk.Application.Abstractions;
using SupportDesk.Application.Contracts;
using SupportDesk.Domain;
using SupportDesk.Domain.Common;
using SupportDesk.Domain.Permissions;
using SupportDesk.Domain.Sla;
using SupportDesk.Domain.Tickets;

namespace SupportDesk.Application.Services;

public sealed partial class TicketService
{
    private readonly IAppDbContext _dbContext;
    private readonly IClock _clock;
    private readonly ICurrentUser _currentUser;
    private readonly IUserDirectory _users;
    private readonly INotificationService _notifications;
    private readonly IOutboxWriter _outbox;
    private readonly ITicketNumberGenerator _numbers;
    private readonly IFileStorage _fileStorage;

    public TicketService(
        IAppDbContext dbContext,
        IClock clock,
        ICurrentUser currentUser,
        IUserDirectory users,
        INotificationService notifications,
        IOutboxWriter outbox,
        ITicketNumberGenerator numbers,
        IFileStorage fileStorage)
    {
        _dbContext = dbContext;
        _clock = clock;
        _currentUser = currentUser;
        _users = users;
        _notifications = notifications;
        _outbox = outbox;
        _numbers = numbers;
        _fileStorage = fileStorage;
    }

    public async Task<TicketDetailDto> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var ticket = await LoadDetailAsync(id, cancellationToken);
        await EnsureCanReadAsync(ticket, cancellationToken);
        return await MapDetailAsync(ticket, cancellationToken);
    }

    public async Task<TicketDetailDto> CreateAsync(CreateTicketRequest request, CancellationToken cancellationToken)
    {
        var userId = RequireAuthenticated();
        RequirePermission(Permissions.TicketsCreate);

        var now = _clock.UtcNow;
        var policy = await _dbContext.SlaPolicies
            .Include(x => x.BusinessCalendar)
            .ThenInclude(x => x!.Holidays)
            .Where(x => x.IsActive && x.Priority == request.Priority)
            .OrderByDescending(x => x.Version)
            .FirstOrDefaultAsync(cancellationToken);

        SlaCommitment? commitment = null;
        if (policy?.BusinessCalendar is not null)
        {
            commitment = SlaCalculator.CreateCommitment(policy, policy.BusinessCalendar, now);
        }

        var number = await _numbers.NextAsync(cancellationToken);
        var ticket = Ticket.Create(number, request.Subject, request.Description, userId, request.Priority, commitment, now);

        if (request.CategoryId is not null)
        {
            ticket.CategoryId = request.CategoryId;
        }

        _dbContext.Tickets.Add(ticket);
        _dbContext.TicketActivities.Add(TicketActivity.Record(ticket.Id, userId, ActivityType.Created, now, correlationId: _currentUser.CorrelationId));

        if (request.Tags is { Count: > 0 })
        {
            await ApplyTagsAsync(ticket, request.Tags, now, cancellationToken);
        }

        await SaveChangesAsync(cancellationToken);
        return await GetAsync(ticket.Id, cancellationToken);
    }

    private async Task<Ticket> LoadDetailAsync(Guid id, CancellationToken cancellationToken)
    {
        var ticket = await _dbContext.Tickets
            .Include(x => x.Category)
            .Include(x => x.SlaPolicy)
            .Include(x => x.Comments).ThenInclude(x => x.Attachments)
            .Include(x => x.Attachments)
            .Include(x => x.Activities)
            .Include(x => x.TicketTags).ThenInclude(x => x.Tag)
            .Include(x => x.ArticleLinks).ThenInclude(x => x.Article)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        return ticket ?? throw new NotFoundException("Ticket not found.");
    }

    private Task EnsureCanReadAsync(Ticket ticket, CancellationToken cancellationToken)
    {
        var userId = RequireAuthenticated();
        if (_currentUser.HasPermission(Permissions.TicketsViewAll))
        {
            return Task.CompletedTask;
        }

        if (_currentUser.HasPermission(Permissions.TicketsViewOwn) && ticket.RequesterId == userId)
        {
            return Task.CompletedTask;
        }

        throw new ForbiddenException("You do not have access to this ticket.");
    }

    private async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException("The ticket was changed by another user. Reload it and try again.");
        }
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

    private static bool IsOverdue(Ticket ticket, DateTimeOffset now)
    {
        if (!TicketWorkflow.IsActive(ticket.Status))
        {
            return false;
        }

        return (ticket.FirstRespondedAt is null && ticket.FirstResponseDueAt is not null && ticket.FirstResponseDueAt < now)
            || (ticket.ResolutionDueAt is not null && ticket.ResolutionDueAt < now);
    }

    private static bool IsDueSoon(Ticket ticket, int dueSoonMinutes, DateTimeOffset now)
    {
        if (!TicketWorkflow.IsActive(ticket.Status))
        {
            return false;
        }

        var warning = now.AddMinutes(Math.Max(1, dueSoonMinutes));
        return (ticket.FirstRespondedAt is null && ticket.FirstResponseDueAt is not null && ticket.FirstResponseDueAt <= warning && ticket.FirstResponseDueAt >= now)
            || (ticket.ResolutionDueAt is not null && ticket.ResolutionDueAt <= warning && ticket.ResolutionDueAt >= now);
    }
}
