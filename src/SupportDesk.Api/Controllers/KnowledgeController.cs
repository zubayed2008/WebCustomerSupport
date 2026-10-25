using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SupportDesk.Application.Contracts;
using SupportDesk.Application.Services;

namespace SupportDesk.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/knowledge")]
public sealed class KnowledgeController : ControllerBase
{
    private readonly KnowledgeService _knowledge;

    public KnowledgeController(KnowledgeService knowledge)
    {
        _knowledge = knowledge;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<KbArticleListItemDto>>> List([FromQuery] KbArticleQuery query, CancellationToken cancellationToken)
    {
        return Ok(await _knowledge.ListAsync(query, cancellationToken));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<KbArticleDto>> Get(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await _knowledge.GetAsync(id, cancellationToken));
    }

    [HttpPost]
    public async Task<ActionResult<KbArticleDto>> Create(CreateKbArticleRequest request, CancellationToken cancellationToken)
    {
        var article = await _knowledge.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = article.Id }, article);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<KbArticleDto>> Update(Guid id, UpdateKbArticleRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _knowledge.UpdateAsync(id, request, cancellationToken));
    }

    [HttpPost("{id:guid}/publish")]
    public async Task<ActionResult<KbArticleDto>> Publish(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await _knowledge.PublishAsync(id, cancellationToken));
    }

    [HttpPost("{id:guid}/archive")]
    public async Task<ActionResult<KbArticleDto>> Archive(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await _knowledge.ArchiveAsync(id, cancellationToken));
    }
}
