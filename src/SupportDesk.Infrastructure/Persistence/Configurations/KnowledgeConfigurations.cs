using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SupportDesk.Domain.Knowledge;

namespace SupportDesk.Infrastructure.Persistence.Configurations;

public sealed class KbArticleConfiguration : IEntityTypeConfiguration<KbArticle>
{
    public void Configure(EntityTypeBuilder<KbArticle> builder)
    {
        builder.ToTable("KbArticles");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Slug).HasMaxLength(160).IsRequired();
        builder.Property(x => x.Title).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Summary).HasMaxLength(500);
        builder.Property(x => x.Body).HasMaxLength(50000).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.Visibility).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.RowVersion).IsRowVersion();
        builder.HasIndex(x => x.Slug).IsUnique();
        builder.HasIndex(x => new { x.Status, x.Visibility });
        builder.HasMany(x => x.Revisions).WithOne(x => x.Article!).HasForeignKey(x => x.ArticleId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(x => x.TicketLinks).WithOne(x => x.Article!).HasForeignKey(x => x.ArticleId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class KbArticleRevisionConfiguration : IEntityTypeConfiguration<KbArticleRevision>
{
    public void Configure(EntityTypeBuilder<KbArticleRevision> builder)
    {
        builder.ToTable("KbArticleRevisions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Title).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Body).HasMaxLength(50000).IsRequired();
        builder.HasIndex(x => x.ArticleId);
    }
}

public sealed class ArticleTicketLinkConfiguration : IEntityTypeConfiguration<ArticleTicketLink>
{
    public void Configure(EntityTypeBuilder<ArticleTicketLink> builder)
    {
        builder.ToTable("ArticleTicketLinks");
        builder.HasKey(x => new { x.ArticleId, x.TicketId });
        builder.HasIndex(x => x.TicketId);
    }
}
