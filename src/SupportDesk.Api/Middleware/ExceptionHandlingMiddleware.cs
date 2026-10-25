using System.Text.Json;
using SupportDesk.Domain.Common;

namespace SupportDesk.Api.Middleware;

/// <summary>
/// Converts domain and unexpected failures into RFC 7807 problem responses.
/// Unexpected exception details are logged but never returned to the client.
/// </summary>
public sealed class ExceptionHandlingMiddleware
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly bool _includeDetails;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger, IWebHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _includeDetails = !environment.IsProduction();
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception exception)
        {
            if (context.Response.HasStarted)
            {
                throw;
            }

            var (status, title) = exception switch
            {
                ValidationFailedException => (StatusCodes.Status400BadRequest, "The request is not valid."),
                NotFoundException => (StatusCodes.Status404NotFound, "The requested resource was not found."),
                ForbiddenException => (StatusCodes.Status403Forbidden, "You do not have permission to perform this action."),
                ConflictException => (StatusCodes.Status409Conflict, "The request conflicts with the current state."),
                DomainException => (StatusCodes.Status400BadRequest, "The request could not be completed."),
                _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred."),
            };

            if (status == StatusCodes.Status500InternalServerError)
            {
                _logger.LogError(exception, "Unhandled exception for {Method} {Path}. CorrelationId: {CorrelationId}", context.Request.Method, context.Request.Path, context.TraceIdentifier);
            }
            else
            {
                _logger.LogInformation("Request rejected with {Status} for {Method} {Path}. CorrelationId: {CorrelationId}", status, context.Request.Method, context.Request.Path, context.TraceIdentifier);
            }

            context.Response.Clear();
            context.Response.StatusCode = status;
            context.Response.ContentType = "application/problem+json";

            var problem = new
            {
                type = $"https://httpstatuses.com/{status}",
                title,
                status,
                detail = status == StatusCodes.Status500InternalServerError && !_includeDetails ? null : exception.Message,
                traceId = context.TraceIdentifier,
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(problem, JsonOptions));
        }
    }
}
