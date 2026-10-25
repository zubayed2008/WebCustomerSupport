using System.ComponentModel.DataAnnotations;
using SupportDesk.Domain;

namespace SupportDesk.Application.Contracts;

public sealed record TicketQuery(
    string? Status = null,
    string? Queue = null,
    TicketPriority? Priority = null,
    Guid? CategoryId = null,
    Guid? AssigneeId = null,
    string? Search = null,
    int Page = 1,
    int PageSize = 25);

public sealed record CreateTicketRequest(
    [Required, MaxLength(200)] string Subject,
    [Required, MaxLength(20000)] string Description,
    TicketPriority Priority = TicketPriority.Normal,
    Guid? CategoryId = null,
    IReadOnlyList<string>? Tags = null);

public sealed record UpdateTicketRequest(
    TicketPriority? Priority,
    Guid? CategoryId,
    Guid? AssigneeId,
    IReadOnlyList<string>? Tags);

public sealed record TransitionTicketRequest(TicketStatus Status);

public sealed record AssignTicketRequest(Guid? AssigneeId);

public sealed record AddCommentRequest(
    [Required, MaxLength(20000)] string Body,
    bool IsInternal = false);

public sealed record TicketListItemDto(
    Guid Id,
    string Number,
    string Subject,
    TicketStatus Status,
    TicketPriority Priority,
    string? CategoryName,
    Guid RequesterId,
    string RequesterName,
    Guid? AssigneeId,
    string? AssigneeName,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? FirstResponseDueAt,
    DateTimeOffset? ResolutionDueAt,
    bool IsDueSoon,
    bool IsOverdue,
    int CommentCount);

public sealed record TicketDetailDto(
    Guid Id,
    string Number,
    string Subject,
    string Description,
    TicketStatus Status,
    TicketPriority Priority,
    Guid? CategoryId,
    string? CategoryName,
    Guid RequesterId,
    string RequesterName,
    Guid? AssigneeId,
    string? AssigneeName,
    IReadOnlyList<string> Tags,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? FirstResponseDueAt,
    DateTimeOffset? ResolutionDueAt,
    DateTimeOffset? FirstRespondedAt,
    DateTimeOffset? ResolvedAt,
    DateTimeOffset? ClosedAt,
    bool IsDueSoon,
    bool IsOverdue,
    bool ResponseBreached,
    bool ResolutionBreached,
    int PausedMinutes,
    uint RowVersion,
    IReadOnlyList<TicketCommentDto> Comments,
    IReadOnlyList<TicketActivityDto> Activities,
    IReadOnlyList<TicketAttachmentDto> Attachments,
    IReadOnlyList<LinkedArticleDto> LinkedArticles);

public sealed record LinkedArticleDto(Guid Id, string Title, string Slug, KbArticleStatus Status);

public sealed record TicketCommentDto(
    Guid Id,
    Guid AuthorId,
    string AuthorName,
    string Body,
    CommentVisibility Visibility,
    bool IsFirstResponse,
    DateTimeOffset CreatedAt,
    IReadOnlyList<TicketAttachmentDto> Attachments);

public sealed record TicketActivityDto(
    Guid Id,
    ActivityType Type,
    Guid? ActorId,
    string? ActorName,
    string? Field,
    string? OldValue,
    string? NewValue,
    DateTimeOffset CreatedAt);

public sealed record TicketAttachmentDto(
    Guid Id,
    string FileName,
    string ContentType,
    long SizeBytes,
    Guid UploadedById,
    DateTimeOffset CreatedAt);

