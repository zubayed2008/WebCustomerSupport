using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SupportDesk.Domain.Ai;
using SupportDesk.Domain.Auditing;
using SupportDesk.Domain.Notifications;
using SupportDesk.Domain.Outbox;
using SupportDesk.Domain.Permissions;

namespace SupportDesk.Infrastructure.Persistence.Configurations;

public sealed class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("Notifications");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Type).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.Title).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Body).HasMaxLength(2000);
        builder.Property(x => x.Link).HasMaxLength(500);
        builder.HasIndex(x => new { x.RecipientId, x.IsRead, x.CreatedAt });
    }
}

public sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("OutboxMessages");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Type).HasMaxLength(200).IsRequired();
        builder.Property(x => x.PayloadJson).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.LastError).HasMaxLength(2000);
        builder.Property(x => x.IdempotencyKey).HasMaxLength(200).IsRequired();
        builder.HasIndex(x => x.IdempotencyKey).IsUnique();
        builder.HasIndex(x => new { x.Status, x.NextAttemptAt });
    }
}

public sealed class AiSuggestionConfiguration : IEntityTypeConfiguration<AiSuggestion>
{
    public void Configure(EntityTypeBuilder<AiSuggestion> builder)
    {
        builder.ToTable("AiSuggestions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Kind).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.InputRevision).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Provider).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Model).HasMaxLength(150);
        builder.Property(x => x.PromptVersion).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Error).HasMaxLength(2000);
        builder.Property(x => x.FeedbackComment).HasMaxLength(1000);
        builder.HasIndex(x => new { x.TicketId, x.CreatedAt });
        builder.HasIndex(x => x.Status);
    }
}

public sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLogs");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Action).HasMaxLength(150).IsRequired();
        builder.Property(x => x.EntityType).HasMaxLength(150).IsRequired();
        builder.Property(x => x.EntityId).HasMaxLength(100).IsRequired();
        builder.Property(x => x.IpAddress).HasMaxLength(64);
        builder.Property(x => x.CorrelationId).HasMaxLength(100);
        builder.HasIndex(x => new { x.EntityType, x.EntityId });
        builder.HasIndex(x => x.CreatedAt);
    }
}

public sealed class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        builder.ToTable("Permissions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Group).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(500).IsRequired();
        builder.HasIndex(x => x.Name).IsUnique();
    }
}
