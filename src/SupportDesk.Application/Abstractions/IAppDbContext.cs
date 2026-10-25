using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using SupportDesk.Domain.Ai;
using SupportDesk.Domain.Auditing;
using SupportDesk.Domain.Knowledge;
using SupportDesk.Domain.Notifications;
using SupportDesk.Domain.Outbox;
using SupportDesk.Domain.Permissions;
using SupportDesk.Domain.Sla;
using SupportDesk.Domain.Tickets;

namespace SupportDesk.Application.Abstractions;

public interface IAppDbContext
{
    DbSet<Ticket> Tickets { get; }

    DbSet<TicketComment> TicketComments { get; }

    DbSet<TicketAttachment> TicketAttachments { get; }

    DbSet<TicketActivity> TicketActivities { get; }

    DbSet<Category> Categories { get; }

    DbSet<Tag> Tags { get; }

    DbSet<TicketTag> TicketTags { get; }

    DbSet<BusinessCalendar> BusinessCalendars { get; }

    DbSet<Holiday> Holidays { get; }

    DbSet<SlaPolicy> SlaPolicies { get; }

    DbSet<KbArticle> KbArticles { get; }

    DbSet<KbArticleRevision> KbArticleRevisions { get; }

    DbSet<ArticleTicketLink> ArticleTicketLinks { get; }

    DbSet<Notification> Notifications { get; }

    DbSet<OutboxMessage> OutboxMessages { get; }

    DbSet<AiSuggestion> AiSuggestions { get; }

    DbSet<AuditLog> AuditLogs { get; }

    DbSet<Permission> Permissions { get; }

    DatabaseFacade Database { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
