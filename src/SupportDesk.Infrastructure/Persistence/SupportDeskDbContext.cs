using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SupportDesk.Application.Abstractions;
using SupportDesk.Domain.Ai;
using SupportDesk.Domain.Auditing;
using SupportDesk.Domain.Knowledge;
using SupportDesk.Domain.Notifications;
using SupportDesk.Domain.Outbox;
using SupportDesk.Domain.Permissions;
using SupportDesk.Domain.Sla;
using SupportDesk.Domain.Tickets;
using SupportDesk.Infrastructure.Identity;

namespace SupportDesk.Infrastructure.Persistence;

public sealed class SupportDeskDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>, IAppDbContext
{
    public SupportDeskDbContext(DbContextOptions<SupportDeskDbContext> options) : base(options)
    {
    }

    public DbSet<Ticket> Tickets => Set<Ticket>();

    public DbSet<TicketComment> TicketComments => Set<TicketComment>();

    public DbSet<TicketAttachment> TicketAttachments => Set<TicketAttachment>();

    public DbSet<TicketActivity> TicketActivities => Set<TicketActivity>();

    public DbSet<Category> Categories => Set<Category>();

    public DbSet<Tag> Tags => Set<Tag>();

    public DbSet<TicketTag> TicketTags => Set<TicketTag>();

    public DbSet<BusinessCalendar> BusinessCalendars => Set<BusinessCalendar>();

    public DbSet<Holiday> Holidays => Set<Holiday>();

    public DbSet<SlaPolicy> SlaPolicies => Set<SlaPolicy>();

    public DbSet<KbArticle> KbArticles => Set<KbArticle>();

    public DbSet<KbArticleRevision> KbArticleRevisions => Set<KbArticleRevision>();

    public DbSet<ArticleTicketLink> ArticleTicketLinks => Set<ArticleTicketLink>();

    public DbSet<Notification> Notifications => Set<Notification>();

    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    public DbSet<AiSuggestion> AiSuggestions => Set<AiSuggestion>();

    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    public DbSet<Permission> Permissions => Set<Permission>();

    public DbSet<ApplicationRolePermission> RolePermissions => Set<ApplicationRolePermission>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.HasSequence<long>("ticket_number_seq").StartsAt(1000).IncrementsBy(1);
        builder.ApplyConfigurationsFromAssembly(typeof(SupportDeskDbContext).Assembly);

        builder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(x => x.DisplayName).HasMaxLength(150).IsRequired();
            entity.HasIndex(x => x.IsActive);
        });

        builder.Entity<ApplicationRole>(entity =>
        {
            entity.Property(x => x.IsSystem);
        });

        builder.Entity<ApplicationRolePermission>(entity =>
        {
            entity.ToTable("AspNetRolePermissions");
            entity.HasKey(x => new { x.RoleId, x.PermissionId });
            entity.HasOne(x => x.Role).WithMany(x => x.RolePermissions).HasForeignKey(x => x.RoleId);
            entity.HasOne(x => x.Permission).WithMany().HasForeignKey(x => x.PermissionId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}
