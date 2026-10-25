using SupportDesk.Application.Abstractions;
using SupportDesk.Domain;
using SupportDesk.Domain.Notifications;
using SupportDesk.Domain.Tickets;

namespace SupportDesk.Application.Services;

public sealed record EmailNotificationPayload(string To, string Subject, string HtmlBody, string? TextBody);

public interface INotificationService
{
    Task NotifyAssignedAsync(Ticket ticket, Guid recipientId, CancellationToken cancellationToken);

    Task NotifyReplyAsync(Ticket ticket, Guid recipientId, string authorName, bool isInternal, CancellationToken cancellationToken);

    Task NotifyStatusChangedAsync(Ticket ticket, Guid recipientId, TicketStatus previous, CancellationToken cancellationToken);

    Task NotifySlaAsync(Ticket ticket, Guid recipientId, bool overdue, CancellationToken cancellationToken);
}

public sealed class NotificationService : INotificationService
{
    private readonly IAppDbContext _dbContext;
    private readonly IClock _clock;
    private readonly IUserDirectory _users;
    private readonly IOutboxWriter _outbox;
    private readonly string _publicUrl;

    public NotificationService(
        IAppDbContext dbContext,
        IClock clock,
        IUserDirectory users,
        IOutboxWriter outbox,
        Microsoft.Extensions.Options.IOptions<PublicUrlOptions> publicUrl)
    {
        _dbContext = dbContext;
        _clock = clock;
        _users = users;
        _outbox = outbox;
        _publicUrl = publicUrl.Value.Url.TrimEnd('/');
    }

    public Task NotifyAssignedAsync(Ticket ticket, Guid recipientId, CancellationToken cancellationToken)
    {
        return CreateAsync(
            recipientId,
            NotificationType.TicketAssigned,
            $"Ticket {ticket.Number} assigned to you",
            ticket.Subject,
            $"/tickets/{ticket.Id}",
            "TicketAssigned",
            cancellationToken);
    }

    public Task NotifyReplyAsync(Ticket ticket, Guid recipientId, string authorName, bool isInternal, CancellationToken cancellationToken)
    {
        var title = isInternal
            ? $"Internal note added to {ticket.Number}"
            : $"New reply on {ticket.Number}";
        var body = $"{authorName}: {ticket.Subject}";

        return CreateAsync(recipientId, NotificationType.TicketReplied, title, body, $"/tickets/{ticket.Id}", "TicketReplied", cancellationToken);
    }

    public Task NotifyStatusChangedAsync(Ticket ticket, Guid recipientId, TicketStatus previous, CancellationToken cancellationToken)
    {
        var title = $"Ticket {ticket.Number} moved to {ticket.Status}";
        return CreateAsync(recipientId, NotificationType.TicketStatusChanged, title, ticket.Subject, $"/tickets/{ticket.Id}", "TicketStatusChanged", cancellationToken);
    }

    public Task NotifySlaAsync(Ticket ticket, Guid recipientId, bool overdue, CancellationToken cancellationToken)
    {
        var title = overdue ? $"SLA breached on {ticket.Number}" : $"SLA due soon on {ticket.Number}";
        return CreateAsync(
            recipientId,
            overdue ? NotificationType.TicketOverdue : NotificationType.TicketDueSoon,
            title,
            ticket.Subject,
            $"/tickets/{ticket.Id}",
            overdue ? "TicketOverdue" : "TicketDueSoon",
            cancellationToken);
    }

    private async Task CreateAsync(
        Guid recipientId,
        NotificationType type,
        string title,
        string body,
        string link,
        string eventName,
        CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow;
        var notification = new Notification
        {
            RecipientId = recipientId,
            Type = type,
            Title = title,
            Body = body,
            Link = link,
            CreatedAt = now,
            UpdatedAt = now,
        };

        _dbContext.Notifications.Add(notification);

        var emails = await _users.GetEmailsAsync([recipientId], cancellationToken);
        if (emails.TryGetValue(recipientId, out var email))
        {
            var payload = new EmailNotificationPayload(
                email,
                title,
                $"<p>{System.Net.WebUtility.HtmlEncode(body)}</p><p><a href=\"{_publicUrl}{link}\">Open ticket</a></p>",
                $"{body}\n{_publicUrl}{link}");

            await _outbox.EnqueueAsync("email.send", payload, $"notification:{notification.Id}:{eventName}", cancellationToken);
        }
    }
}

public sealed class PublicUrlOptions
{
    public string Url { get; set; } = "http://localhost:8080";
}
