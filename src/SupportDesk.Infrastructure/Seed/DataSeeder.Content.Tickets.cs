using Microsoft.EntityFrameworkCore;
using SupportDesk.Domain;
using SupportDesk.Domain.Knowledge;
using SupportDesk.Domain.Sla;
using SupportDesk.Domain.Tickets;

namespace SupportDesk.Infrastructure.Seed;

public sealed partial class DataSeeder
{
    private async Task AddRemainingDemoTicketsAsync(
        List<Ticket> tickets,
        Guid customerId,
        Guid agentId,
        BusinessCalendar calendar,
        IReadOnlyList<SlaPolicy> policies,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var overdueTicket = Ticket.Create("SD-000004", "Production login outage for all users", "No user can sign in to the production workspace. This is blocking all work.", customerId, TicketPriority.Urgent, Commitment(policies, calendar, TicketPriority.Urgent, now), now.AddHours(-10));
        overdueTicket.Transition(TicketStatus.Open, now.AddHours(-9.5));
        overdueTicket.Assign(agentId, now.AddHours(-9.5));
        overdueTicket.FirstResponseDueAt = now.AddHours(-9);
        overdueTicket.ResolutionDueAt = now.AddHours(-6);
        overdueTicket.MarkResponseBreached(now.AddHours(-8));
        tickets.Add(overdueTicket);

        var resolvedTicket = Ticket.Create("SD-000005", "Password reset email never arrives", "I requested a password reset three times and no email arrived.", customerId, TicketPriority.Normal, Commitment(policies, calendar, TicketPriority.Normal, now), now.AddDays(-5));
        resolvedTicket.Transition(TicketStatus.Open, now.AddDays(-5));
        resolvedTicket.Assign(agentId, now.AddDays(-5));
        resolvedTicket.RecordPublicResponse(now.AddDays(-4.9));
        resolvedTicket.Transition(TicketStatus.Resolved, now.AddDays(-4));
        tickets.Add(resolvedTicket);

        foreach (var ticket in tickets)
        {
            _dbContext.Tickets.Add(ticket);
            _dbContext.TicketActivities.Add(TicketActivity.Record(ticket.Id, customerId, ActivityType.Created, ticket.CreatedAt, correlationId: "seed"));
        }

        var openTicket = tickets.First(x => x.Number == "SD-000002");
        var pendingTicket = tickets.First(x => x.Number == "SD-000003");

        _dbContext.TicketComments.AddRange(
            TicketComment.Create(openTicket.Id, agentId, "Thank you for reporting this. We are reviewing the gateway logs and will update you shortly.", CommentVisibility.Public, true, now.AddHours(-6)),
            TicketComment.Create(openTicket.Id, agentId, "Correlation IDs captured. Gateway vendor ticket opened.", CommentVisibility.Internal, false, now.AddHours(-5)),
            TicketComment.Create(pendingTicket.Id, agentId, "Could you confirm which email address should receive the invitation?", CommentVisibility.Public, true, now.AddDays(-1.9)),
            TicketComment.Create(overdueTicket.Id, agentId, "We are treating this as a production outage and escalating now.", CommentVisibility.Public, true, now.AddHours(-9)),
            TicketComment.Create(resolvedTicket.Id, agentId, "The email provider had suppressed the address. We cleared the suppression and confirmed delivery.", CommentVisibility.Public, true, now.AddDays(-4.9)));

        var resetArticle = await _dbContext.KbArticles.FirstOrDefaultAsync(x => x.Slug == "reset-your-password", cancellationToken);
        if (resetArticle is not null)
        {
            _dbContext.ArticleTicketLinks.Add(new ArticleTicketLink { TicketId = resolvedTicket.Id, ArticleId = resetArticle.Id });
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
