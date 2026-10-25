using SupportDesk.Application.Abstractions;
using SupportDesk.Application.Contracts;
using SupportDesk.Application.Validation;
using SupportDesk.Domain;
using SupportDesk.Domain.Common;
using SupportDesk.Domain.Permissions;
using SupportDesk.Domain.Tickets;

namespace SupportDesk.Application.Services;

public sealed record AttachmentDownload(Stream Content, string FileName, string ContentType);

public sealed partial class TicketService
{
    private const int MaxSignatureBytes = 16;

    public async Task<TicketDetailDto> AddAttachmentAsync(
        Guid ticketId,
        Stream content,
        string fileName,
        string contentType,
        long sizeBytes,
        Guid? commentId,
        CancellationToken cancellationToken)
    {
        var userId = RequireAuthenticated();
        RequirePermission(Permissions.TicketsAttach);
        var ticket = await LoadDetailAsync(ticketId, cancellationToken);
        await EnsureCanReadAsync(ticket, cancellationToken);

        if (commentId is not null)
        {
            var comment = ticket.Comments.FirstOrDefault(x => x.Id == commentId)
                ?? throw new NotFoundException("Comment not found on this ticket.");
            if (comment.Visibility == CommentVisibility.Internal && !_currentUser.HasPermission(Permissions.TicketsNote))
            {
                throw new ForbiddenException("You cannot attach files to an internal note.");
            }
        }

        await using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, cancellationToken);
        if (buffer.Length > FileUploadValidator.MaxSizeBytes)
        {
            throw new ValidationFailedException("Attachments cannot exceed 10 MB.");
        }

        buffer.Position = 0;
        var header = new byte[Math.Min(MaxSignatureBytes, (int)Math.Min(buffer.Length, MaxSignatureBytes))];
        _ = await buffer.ReadAsync(header, cancellationToken);
        buffer.Position = 0;

        var normalizedContentType = FileUploadValidator.Validate(fileName, contentType, buffer.Length, header);
        var safeName = Path.GetFileName(fileName);
        var stored = await _fileStorage.SaveAsync(buffer, safeName, normalizedContentType, cancellationToken);

        var now = _clock.UtcNow;
        var attachment = TicketAttachment.Create(
            ticket.Id,
            commentId,
            stored.StorageKey,
            safeName,
            normalizedContentType,
            stored.SizeBytes,
            stored.Sha256,
            userId,
            now);

        _dbContext.TicketAttachments.Add(attachment);
        ticket.Touch(now);
        _dbContext.TicketActivities.Add(TicketActivity.Record(
            ticket.Id,
            userId,
            ActivityType.AttachmentAdded,
            now,
            field: "Attachment",
            newValue: safeName,
            correlationId: _currentUser.CorrelationId));

        await SaveChangesAsync(cancellationToken);
        return await GetAsync(ticket.Id, cancellationToken);
    }

    public async Task<AttachmentDownload> DownloadAttachmentAsync(Guid ticketId, Guid attachmentId, CancellationToken cancellationToken)
    {
        var ticket = await LoadDetailAsync(ticketId, cancellationToken);
        await EnsureCanReadAsync(ticket, cancellationToken);
        RequirePermission(Permissions.TicketsAttach);

        var attachment = ticket.Attachments.FirstOrDefault(x => x.Id == attachmentId)
            ?? throw new NotFoundException("Attachment not found.");

        var canViewAll = _currentUser.HasPermission(Permissions.TicketsViewAll);
        if (!canViewAll && attachment.CommentId is not null)
        {
            var comment = ticket.Comments.FirstOrDefault(x => x.Id == attachment.CommentId);
            if (comment is null || comment.Visibility == CommentVisibility.Internal)
            {
                throw new ForbiddenException("You do not have access to this attachment.");
            }
        }

        var stream = await _fileStorage.OpenReadAsync(attachment.StorageKey, cancellationToken);
        return new AttachmentDownload(stream, attachment.FileName, attachment.ContentType);
    }
}
