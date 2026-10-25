using SupportDesk.Domain.Common;

namespace SupportDesk.Domain.Tickets;

public sealed class TicketAttachment : Entity
{
    public Guid TicketId { get; set; }

    public Ticket? Ticket { get; set; }

    public Guid? CommentId { get; set; }

    public TicketComment? Comment { get; set; }

    public string StorageKey { get; set; } = string.Empty;

    public string FileName { get; set; } = string.Empty;

    public string ContentType { get; set; } = string.Empty;

    public long SizeBytes { get; set; }

    public string Sha256 { get; set; } = string.Empty;

    public Guid UploadedById { get; set; }

    public static TicketAttachment Create(
        Guid ticketId,
        Guid? commentId,
        string storageKey,
        string fileName,
        string contentType,
        long sizeBytes,
        string sha256,
        Guid uploadedById,
        DateTimeOffset now)
    {
        return new TicketAttachment
        {
            TicketId = ticketId,
            CommentId = commentId,
            StorageKey = storageKey,
            FileName = fileName,
            ContentType = contentType,
            SizeBytes = sizeBytes,
            Sha256 = sha256,
            UploadedById = uploadedById,
            CreatedAt = now,
            UpdatedAt = now,
        };
    }
}
