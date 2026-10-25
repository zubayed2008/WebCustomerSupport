using System.ComponentModel.DataAnnotations;
using SupportDesk.Domain;

namespace SupportDesk.Application.Contracts;

public sealed record KbArticleQuery(
    string? Search = null,
    KbArticleStatus? Status = null,
    int Page = 1,
    int PageSize = 25);

public sealed record KbArticleListItemDto(
    Guid Id,
    string Slug,
    string Title,
    string Summary,
    KbArticleStatus Status,
    KbVisibility Visibility,
    string AuthorName,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? PublishedAt);

public sealed record KbArticleDto(
    Guid Id,
    string Slug,
    string Title,
    string Summary,
    string Body,
    KbArticleStatus Status,
    KbVisibility Visibility,
    Guid AuthorId,
    string AuthorName,
    Guid? PublisherId,
    DateTimeOffset? PublishedAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    uint RowVersion);

public sealed record CreateKbArticleRequest(
    [Required, MaxLength(200)] string Title,
    [MaxLength(500)] string Summary,
    [Required, MaxLength(50000)] string Body,
    KbVisibility Visibility = KbVisibility.Internal,
    string? Slug = null);

public sealed record UpdateKbArticleRequest(
    [Required, MaxLength(200)] string Title,
    [MaxLength(500)] string Summary,
    [Required, MaxLength(50000)] string Body,
    KbVisibility Visibility);

public sealed record LinkArticleRequest(Guid ArticleId);

