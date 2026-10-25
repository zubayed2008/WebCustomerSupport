using Microsoft.EntityFrameworkCore;
using SupportDesk.Domain;
using SupportDesk.Domain.Knowledge;
using SupportDesk.Domain.Sla;
using SupportDesk.Domain.Tickets;

namespace SupportDesk.Infrastructure.Seed;

public sealed partial class DataSeeder
{
    private async Task SeedDemoContentAsync(DateTimeOffset now, CancellationToken cancellationToken)
    {
        var customer = await _userManager.FindByEmailAsync("customer@example.com");
        var agent = await _userManager.FindByEmailAsync("agent@example.com");
        var admin = await _userManager.FindByEmailAsync(_options.AdminEmail);
        if (customer is null || agent is null || admin is null)
        {
            return;
        }

        if (!await _dbContext.KbArticles.AnyAsync(cancellationToken))
        {
            var resetArticle = KbArticle.Create(
                "reset-your-password",
                "How to reset your password",
                "Steps to reset a forgotten password from the sign-in page.",
                "Open the sign-in page and choose Forgot password. Enter the email address for your account and follow the link that is sent to you. The link expires after 30 minutes. If you do not receive it, check your spam folder before contacting support.",
                KbVisibility.Customer,
                admin.Id,
                now);
            resetArticle.Publish(admin.Id, now);

            var exportArticle = KbArticle.Create(
                "export-reports",
                "Exporting reports",
                "Why a report export can fail and how to resolve it.",
                "Large exports are generated in the background. If an export fails, reduce the date range, confirm that your account has report access, and retry. Repeated failures should be reported with the exact time of the attempt.",
                KbVisibility.Customer,
                admin.Id,
                now);
            exportArticle.Publish(admin.Id, now);

            var internalArticle = KbArticle.Create(
                "internal-escalation-checklist",
                "Internal escalation checklist",
                "Internal-only escalation steps for urgent tickets.",
                "Confirm the customer impact, verify the affected environment, capture correlation IDs, and notify the on-call manager. Never copy internal notes into a customer reply.",
                KbVisibility.Internal,
                admin.Id,
                now);

            _dbContext.KbArticles.AddRange(resetArticle, exportArticle, internalArticle);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        if (await _dbContext.Tickets.AnyAsync(cancellationToken))
        {
            return;
        }

        var calendar = await _dbContext.BusinessCalendars.Include(x => x.Holidays).FirstAsync(cancellationToken);
        var policies = await _dbContext.SlaPolicies.ToListAsync(cancellationToken);
        var tickets = new List<Ticket>();

        var newTicket = Ticket.Create("SD-000001", "Cannot export the monthly report", "When I export the monthly report, the download never starts. This blocks our month-end review.", customer.Id, TicketPriority.Normal, Commitment(policies, calendar, TicketPriority.Normal, now), now.AddHours(-3));
        tickets.Add(newTicket);

        var openTicket = Ticket.Create("SD-000002", "Integration returns intermittent 502 errors", "Our integration receives 502 responses several times per hour since the latest update.", customer.Id, TicketPriority.High, Commitment(policies, calendar, TicketPriority.High, now), now.AddHours(-8));
        openTicket.Transition(TicketStatus.Open, now.AddHours(-7));
        openTicket.Assign(agent.Id, now.AddHours(-7));
        openTicket.RecordPublicResponse(now.AddHours(-6));
        tickets.Add(openTicket);

        var pendingTicket = Ticket.Create("SD-000003", "Unable to invite a second administrator", "The invite button is disabled for our account and we need a second administrator.", customer.Id, TicketPriority.Normal, Commitment(policies, calendar, TicketPriority.Normal, now), now.AddDays(-2));
        pendingTicket.Transition(TicketStatus.Open, now.AddDays(-2));
        pendingTicket.Assign(agent.Id, now.AddDays(-2));
        pendingTicket.RecordPublicResponse(now.AddDays(-1.9));
        pendingTicket.Transition(TicketStatus.PendingCustomer, now.AddDays(-1.5));
        tickets.Add(pendingTicket);

        await AddRemainingDemoTicketsAsync(tickets, customer.Id, agent.Id, calendar, policies, now, cancellationToken);
    }

    private static SlaCommitment? Commitment(IReadOnlyList<SlaPolicy> policies, BusinessCalendar calendar, TicketPriority priority, DateTimeOffset now)
    {
        var policy = policies.FirstOrDefault(x => x.Priority == priority);
        return policy is null ? null : SlaCalculator.CreateCommitment(policy, calendar, now);
    }
}
