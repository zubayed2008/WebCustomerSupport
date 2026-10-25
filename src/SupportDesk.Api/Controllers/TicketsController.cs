using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SupportDesk.Api.Security;
using SupportDesk.Application.Contracts;
using SupportDesk.Application.Services;
using SupportDesk.Domain;
using SupportDesk.Domain.Permissions;

namespace SupportDesk.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/tickets")]
public sealed class TicketsController : ControllerBase
{
    private const long MaxUploadBytes = 10 * 1024 * 1024;

    private readonly TicketService _tickets;
    private readonly AiSuggestionService _ai;

    public TicketsController(TicketService tickets, AiSuggestionService ai)
    {
        _tickets = tickets;
        _ai = ai;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<TicketListItemDto>>> List([FromQuery] TicketQuery query, CancellationToken cancellationToken)
    {
        return Ok(await _tickets.ListAsync(query, cancellationToken));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TicketDetailDto>> Get(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await _tickets.GetAsync(id, cancellationToken));
    }

    [HttpPost]
    [HasPermission(Permissions.TicketsCreate)]
    public async Task<ActionResult<TicketDetailDto>> Create(CreateTicketRequest request, CancellationToken cancellationToken)
    {
        var ticket = await _tickets.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = ticket.Id }, ticket);
    }

    [HttpPost("{id:guid}/comments")]
    public async Task<ActionResult<TicketDetailDto>> AddComment(Guid id, AddCommentRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _tickets.AddCommentAsync(id, request, cancellationToken));
    }

    [HttpPost("{id:guid}/transitions")]
    public async Task<ActionResult<TicketDetailDto>> Transition(Guid id, TransitionTicketRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _tickets.TransitionAsync(id, request.Status, cancellationToken));
    }

    [HttpPost("{id:guid}/assignment")]
    public async Task<ActionResult<TicketDetailDto>> Assign(Guid id, AssignTicketRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _tickets.AssignAsync(id, request.AssigneeId, cancellationToken));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<TicketDetailDto>> Update(Guid id, UpdateTicketRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _tickets.UpdateAsync(id, request, cancellationToken));
    }

    [HttpPost("{id:guid}/attachments")]
    [RequestSizeLimit(MaxUploadBytes)]
    [HasPermission(Permissions.TicketsAttach)]
    public async Task<ActionResult<TicketDetailDto>> Upload(Guid id, IFormFile file, [FromQuery] Guid? commentId, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return ValidationProblem(new ValidationProblemDetails(new Dictionary<string, string[]> { ["file"] = ["A file is required."] }));
        }

        await using var stream = file.OpenReadStream();
        return Ok(await _tickets.AddAttachmentAsync(id, stream, file.FileName, file.ContentType, file.Length, commentId, cancellationToken));
    }

    [HttpGet("{id:guid}/attachments/{attachmentId:guid}")]
    [HasPermission(Permissions.TicketsAttach)]
    public async Task<IActionResult> Download(Guid id, Guid attachmentId, CancellationToken cancellationToken)
    {
        var download = await _tickets.DownloadAttachmentAsync(id, attachmentId, cancellationToken);
        return File(download.Content, download.ContentType, download.FileName);
    }

    [HttpPost("{id:guid}/articles")]
    public async Task<ActionResult<TicketDetailDto>> LinkArticle(Guid id, LinkArticleRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _tickets.LinkArticleAsync(id, request.ArticleId, cancellationToken));
    }

    [HttpDelete("{id:guid}/articles/{articleId:guid}")]
    public async Task<ActionResult<TicketDetailDto>> UnlinkArticle(Guid id, Guid articleId, CancellationToken cancellationToken)
    {
        return Ok(await _tickets.UnlinkArticleAsync(id, articleId, cancellationToken));
    }

    [HttpGet("{id:guid}/ai-suggestions")]
    [HasPermission(Permissions.AiUse)]
    public async Task<ActionResult<IReadOnlyList<AiSuggestionDto>>> ListAi(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await _ai.ListAsync(id, cancellationToken));
    }

    [HttpPost("{id:guid}/ai-suggestions")]
    [HasPermission(Permissions.AiUse)]
    [EnableRateLimiting("ai")]
    public async Task<ActionResult<AiSuggestionDto>> RequestAi(Guid id, RequestAiSuggestionRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _ai.RequestAsync(id, request.Kind, cancellationToken));
    }
}

[ApiController]
[Authorize]
[Route("api/v1/ai-suggestions")]
public sealed class AiFeedbackController : ControllerBase
{
    private readonly AiSuggestionService _ai;

    public AiFeedbackController(AiSuggestionService ai)
    {
        _ai = ai;
    }

    [HttpPost("{id:guid}/feedback")]
    [HasPermission(Permissions.AiReview)]
    public async Task<ActionResult<AiSuggestionDto>> Feedback(Guid id, AiFeedbackRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _ai.AddFeedbackAsync(id, request, cancellationToken));
    }
}
