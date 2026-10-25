using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SupportDesk.Domain.Tickets;

namespace SupportDesk.Infrastructure.Persistence.Configurations;

public sealed class TicketConfiguration : IEntityTypeConfiguration<Ticket>
{
    public void Configure(EntityTypeBuilder<Ticket> builder)
    {
        builder.ToTable("Tickets");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Number).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Subject).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(20000).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.Priority).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasIndex(x => x.Number).IsUnique();
        builder.HasIndex(x => new { x.Status, x.Priority, x.CreatedAt });
        builder.HasIndex(x => x.AssigneeId);
        builder.HasIndex(x => x.RequesterId);
        builder.HasIndex(x => x.FirstResponseDueAt);
        builder.HasIndex(x => x.ResolutionDueAt);

        builder.HasOne(x => x.Category)
            .WithMany()
            .HasForeignKey(x => x.CategoryId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(x => x.SlaPolicy)
            .WithMany()
            .HasForeignKey(x => x.SlaPolicyId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(x => x.Comments).WithOne(x => x.Ticket!).HasForeignKey(x => x.TicketId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(x => x.Attachments).WithOne(x => x.Ticket!).HasForeignKey(x => x.TicketId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(x => x.Activities).WithOne(x => x.Ticket!).HasForeignKey(x => x.TicketId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(x => x.TicketTags).WithOne(x => x.Ticket!).HasForeignKey(x => x.TicketId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(x => x.ArticleLinks).WithOne(x => x.Ticket!).HasForeignKey(x => x.TicketId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class TicketCommentConfiguration : IEntityTypeConfiguration<TicketComment>
{
    public void Configure(EntityTypeBuilder<TicketComment> builder)
    {
        builder.ToTable("TicketComments");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Body).HasMaxLength(20000).IsRequired();
        builder.Property(x => x.Visibility).HasConversion<string>().HasMaxLength(16);
        builder.HasIndex(x => new { x.TicketId, x.CreatedAt });
        builder.HasMany(x => x.Attachments).WithOne(x => x.Comment!).HasForeignKey(x => x.CommentId).OnDelete(DeleteBehavior.SetNull);
    }
}

public sealed class TicketAttachmentConfiguration : IEntityTypeConfiguration<TicketAttachment>
{
    public void Configure(EntityTypeBuilder<TicketAttachment> builder)
    {
        builder.ToTable("TicketAttachments");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.StorageKey).HasMaxLength(200).IsRequired();
        builder.Property(x => x.FileName).HasMaxLength(255).IsRequired();
        builder.Property(x => x.ContentType).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Sha256).HasMaxLength(64).IsRequired();
        builder.HasIndex(x => x.TicketId);
    }
}

public sealed class TicketActivityConfiguration : IEntityTypeConfiguration<TicketActivity>
{
    public void Configure(EntityTypeBuilder<TicketActivity> builder)
    {
        builder.ToTable("TicketActivities");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Type).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.Field).HasMaxLength(100);
        builder.Property(x => x.OldValue).HasMaxLength(500);
        builder.Property(x => x.NewValue).HasMaxLength(500);
        builder.Property(x => x.CorrelationId).HasMaxLength(100);
        builder.HasIndex(x => new { x.TicketId, x.CreatedAt });
    }
}

public sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("Categories");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(500);
        builder.HasIndex(x => x.Name).IsUnique();
    }
}

public sealed class TagConfiguration : IEntityTypeConfiguration<Tag>
{
    public void Configure(EntityTypeBuilder<Tag> builder)
    {
        builder.ToTable("Tags");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(60).IsRequired();
        builder.Property(x => x.NormalizedName).HasMaxLength(60).IsRequired();
        builder.HasIndex(x => x.NormalizedName).IsUnique();
    }
}

public sealed class TicketTagConfiguration : IEntityTypeConfiguration<TicketTag>
{
    public void Configure(EntityTypeBuilder<TicketTag> builder)
    {
        builder.ToTable("TicketTags");
        builder.HasKey(x => new { x.TicketId, x.TagId });
        builder.HasOne(x => x.Tag).WithMany(x => x.TicketTags).HasForeignKey(x => x.TagId).OnDelete(DeleteBehavior.Cascade);
    }
}
