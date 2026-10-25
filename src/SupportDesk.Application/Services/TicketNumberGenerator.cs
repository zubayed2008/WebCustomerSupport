using Microsoft.EntityFrameworkCore;
using SupportDesk.Application.Abstractions;

namespace SupportDesk.Application.Services;

public interface ITicketNumberGenerator
{
    Task<string> NextAsync(CancellationToken cancellationToken);
}

/// <summary>
/// Uses a PostgreSQL sequence so concurrent requests cannot produce duplicate numbers.
/// </summary>
public sealed class PostgresTicketNumberGenerator : ITicketNumberGenerator
{
    private readonly IAppDbContext _dbContext;

    public PostgresTicketNumberGenerator(IAppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<string> NextAsync(CancellationToken cancellationToken)
    {
        var value = await _dbContext.Database
            .SqlQueryRaw<long>("SELECT nextval('ticket_number_seq') AS \"Value\"")
            .SingleAsync(cancellationToken);

        return $"SD-{value:000000}";
    }
}
