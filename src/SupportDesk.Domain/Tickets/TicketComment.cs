using SupportDesk.Domain.Common;

namespace SupportDesk.Domain.Tickets;

public sealed class TicketComment : Entity
{
    public Guid TicketId { get; set; }

    public Ticket? Ticket { get; set; }

    public Guid AuthorId { get; set; }

    public string Body { get; set; } = string.Empty;

    public CommentVisibility Visibility { get; set; }

    public bool IsFirstResponse { get; set; }

    public ICollection<TicketAttachment> Attachments { get; set; } = [];

    public static TicketComment Create(
        Guid ticketId,
        Guid authorId,
        string body,
        CommentVisibility visibility,
        bool isFirstResponse,
        DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            throw new ValidationFailedException("Comment body is required.");
        }

        if (body.Length > 20000)
        {
            throw new ValidationFailedException("Comment body cannot exceed 20000 characters.");
        }

        return new TicketComment
        {
            TicketId = ticketId,
            AuthorId = authorId,
            Body = body.Trim(),
            Visibility = visibility,
            IsFirstResponse = isFirstResponse,
            CreatedAt = now,
            UpdatedAt = now,
        };
    }
}
