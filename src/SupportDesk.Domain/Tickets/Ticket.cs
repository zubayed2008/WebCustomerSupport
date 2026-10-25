using SupportDesk.Domain.Common;
using SupportDesk.Domain.Knowledge;
using SupportDesk.Domain.Sla;

namespace SupportDesk.Domain.Tickets;

public sealed class Ticket : Entity
{
    public string Number { get; set; } = string.Empty;

    public string Subject { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public Guid RequesterId { get; set; }

    public Guid? AssigneeId { get; set; }

    public Guid? CategoryId { get; set; }

    public Category? Category { get; set; }

    public TicketPriority Priority { get; set; } = TicketPriority.Normal;

    public TicketStatus Status { get; set; } = TicketStatus.New;

    public Guid? SlaPolicyId { get; set; }

    public SlaPolicy? SlaPolicy { get; set; }

    public DateTimeOffset? FirstResponseDueAt { get; set; }

    public DateTimeOffset? ResolutionDueAt { get; set; }

    public DateTimeOffset? FirstRespondedAt { get; set; }

    public DateTimeOffset? ResolvedAt { get; set; }

    public DateTimeOffset? ClosedAt { get; set; }

    public DateTimeOffset? PendingCustomerSince { get; set; }

    public int PausedMinutes { get; set; }

    public bool ResponseBreached { get; set; }

    public bool ResolutionBreached { get; set; }

    public uint RowVersion { get; set; }

    public ICollection<TicketComment> Comments { get; set; } = [];

    public ICollection<TicketAttachment> Attachments { get; set; } = [];

    public ICollection<TicketActivity> Activities { get; set; } = [];

    public ICollection<TicketTag> TicketTags { get; set; } = [];

    public ICollection<ArticleTicketLink> ArticleLinks { get; set; } = [];

    public static Ticket Create(
        string number,
        string subject,
        string description,
        Guid requesterId,
        TicketPriority priority,
        SlaCommitment? commitment,
        DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(subject))
        {
            throw new ValidationFailedException("Ticket subject is required.");
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ValidationFailedException("Ticket description is required.");
        }

        var ticket = new Ticket
        {
            Number = number,
            Subject = subject.Trim(),
            Description = description.Trim(),
            RequesterId = requesterId,
            Priority = priority,
            Status = TicketStatus.New,
            CreatedAt = now,
            UpdatedAt = now,
        };

        if (commitment is not null)
        {
            ticket.SlaPolicyId = commitment.PolicyId;
            ticket.FirstResponseDueAt = commitment.FirstResponseDueAt;
            ticket.ResolutionDueAt = commitment.ResolutionDueAt;
        }

        return ticket;
    }

    public void Transition(TicketStatus target, DateTimeOffset now, int pausedMinutes = 0)
    {
        TicketWorkflow.EnsureTransition(Status, target);
        var previous = Status;

        if (previous == TicketStatus.PendingCustomer && target != TicketStatus.PendingCustomer)
        {
            PausedMinutes += Math.Max(0, pausedMinutes);
            PendingCustomerSince = null;
        }

        if (target == TicketStatus.PendingCustomer)
        {
            PendingCustomerSince = now;
        }

        if (target == TicketStatus.Resolved)
        {
            ResolvedAt = now;
        }
        else if (target == TicketStatus.Closed)
        {
            ClosedAt = now;
        }
        else if (TicketWorkflow.IsReopen(previous, target))
        {
            ResolvedAt = null;
            ClosedAt = null;
            ResolutionBreached = false;
        }

        Status = target;
        UpdatedAt = now;
    }

    public void Assign(Guid? assigneeId, DateTimeOffset now)
    {
        AssigneeId = assigneeId;
        UpdatedAt = now;
    }

    public void ChangePriority(TicketPriority priority, DateTimeOffset now)
    {
        Priority = priority;
        UpdatedAt = now;
    }

    public void ChangeCategory(Guid? categoryId, DateTimeOffset now)
    {
        CategoryId = categoryId;
        UpdatedAt = now;
    }

    public void RecordPublicResponse(DateTimeOffset now)
    {
        FirstRespondedAt ??= now;
        UpdatedAt = now;
    }

    public void MarkResponseBreached(DateTimeOffset now)
    {
        if (!ResponseBreached)
        {
            ResponseBreached = true;
            UpdatedAt = now;
        }
    }

    public void MarkResolutionBreached(DateTimeOffset now)
    {
        if (!ResolutionBreached)
        {
            ResolutionBreached = true;
            UpdatedAt = now;
        }
    }

    public void Touch(DateTimeOffset now)
    {
        UpdatedAt = now;
    }
}
