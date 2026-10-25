using SupportDesk.Domain;
using SupportDesk.Domain.Common;
using SupportDesk.Domain.Tickets;

namespace SupportDesk.UnitTests;

public sealed class TicketWorkflowTests
{
    [Theory]
    [InlineData(TicketStatus.New, TicketStatus.Open, true)]
    [InlineData(TicketStatus.New, TicketStatus.Closed, false)]
    [InlineData(TicketStatus.Open, TicketStatus.Resolved, true)]
    [InlineData(TicketStatus.Resolved, TicketStatus.Open, true)]
    [InlineData(TicketStatus.Closed, TicketStatus.Open, true)]
    [InlineData(TicketStatus.Resolved, TicketStatus.PendingCustomer, false)]
    [InlineData(TicketStatus.Open, TicketStatus.Open, false)]
    public void CanTransition_ReturnsExpectedResult(TicketStatus from, TicketStatus to, bool expected)
    {
        Assert.Equal(expected, TicketWorkflow.CanTransition(from, to));
    }

    [Fact]
    public void EnsureTransition_ThrowsForInvalidTransition()
    {
        Assert.Throws<ConflictException>(() => TicketWorkflow.EnsureTransition(TicketStatus.Closed, TicketStatus.PendingInternal));
    }

    [Fact]
    public void Transition_ToPendingCustomer_RecordsPauseStart()
    {
        var now = new DateTimeOffset(2026, 1, 5, 10, 0, 0, TimeSpan.Zero);
        var ticket = Ticket.Create("SD-1", "Subject", "Description", Guid.NewGuid(), TicketPriority.Normal, null, now);

        ticket.Transition(TicketStatus.Open, now);
        ticket.Transition(TicketStatus.PendingCustomer, now.AddMinutes(30));

        Assert.Equal(TicketStatus.PendingCustomer, ticket.Status);
        Assert.Equal(now.AddMinutes(30), ticket.PendingCustomerSince);
    }

    [Fact]
    public void Transition_FromPendingCustomer_AccumulatesPausedMinutes()
    {
        var now = new DateTimeOffset(2026, 1, 5, 10, 0, 0, TimeSpan.Zero);
        var ticket = Ticket.Create("SD-2", "Subject", "Description", Guid.NewGuid(), TicketPriority.Normal, null, now);
        ticket.Transition(TicketStatus.Open, now);
        ticket.Transition(TicketStatus.PendingCustomer, now);

        ticket.Transition(TicketStatus.Open, now.AddHours(2), pausedMinutes: 120);

        Assert.Equal(120, ticket.PausedMinutes);
        Assert.Null(ticket.PendingCustomerSince);
    }

    [Fact]
    public void Reopen_ClearsResolutionAndClosedTimestamps()
    {
        var now = new DateTimeOffset(2026, 1, 5, 10, 0, 0, TimeSpan.Zero);
        var ticket = Ticket.Create("SD-3", "Subject", "Description", Guid.NewGuid(), TicketPriority.Normal, null, now);
        ticket.Transition(TicketStatus.Open, now);
        ticket.Transition(TicketStatus.Resolved, now.AddHours(1));
        ticket.Transition(TicketStatus.Closed, now.AddHours(2));

        ticket.Transition(TicketStatus.Open, now.AddHours(3));

        Assert.Null(ticket.ResolvedAt);
        Assert.Null(ticket.ClosedAt);
        Assert.Equal(TicketStatus.Open, ticket.Status);
    }

    [Fact]
    public void RecordPublicResponse_OnlySetsFirstResponseOnce()
    {
        var now = new DateTimeOffset(2026, 1, 5, 10, 0, 0, TimeSpan.Zero);
        var ticket = Ticket.Create("SD-4", "Subject", "Description", Guid.NewGuid(), TicketPriority.Normal, null, now);

        ticket.RecordPublicResponse(now.AddMinutes(5));
        ticket.RecordPublicResponse(now.AddMinutes(30));

        Assert.Equal(now.AddMinutes(5), ticket.FirstRespondedAt);
    }
}
