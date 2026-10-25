using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SupportDesk.Domain.Sla;

namespace SupportDesk.Infrastructure.Persistence.Configurations;

public sealed class BusinessCalendarConfiguration : IEntityTypeConfiguration<BusinessCalendar>
{
    public void Configure(EntityTypeBuilder<BusinessCalendar> builder)
    {
        builder.ToTable("BusinessCalendars");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(150).IsRequired();
        builder.Property(x => x.TimeZoneId).HasMaxLength(100).IsRequired();
        builder.HasIndex(x => x.Name).IsUnique();

        builder.OwnsOne(x => x.Schedule, schedule =>
        {
            // ToJson is applied only to the outermost owned type; nested owned
            // collections are stored inside the same JSON document.
            schedule.ToJson();
            schedule.OwnsMany(x => x.Days, day =>
            {
                day.OwnsMany(x => x.Intervals);
            });
        });

        builder.HasMany(x => x.Holidays).WithOne(x => x.BusinessCalendar!).HasForeignKey(x => x.BusinessCalendarId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class HolidayConfiguration : IEntityTypeConfiguration<Holiday>
{
    public void Configure(EntityTypeBuilder<Holiday> builder)
    {
        builder.ToTable("Holidays");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(150).IsRequired();
        builder.HasIndex(x => new { x.BusinessCalendarId, x.Date }).IsUnique();
    }
}

public sealed class SlaPolicyConfiguration : IEntityTypeConfiguration<SlaPolicy>
{
    public void Configure(EntityTypeBuilder<SlaPolicy> builder)
    {
        builder.ToTable("SlaPolicies");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(150).IsRequired();
        builder.Property(x => x.Priority).HasConversion<string>().HasMaxLength(32);
        builder.HasIndex(x => x.Priority).IsUnique().HasFilter("\"IsActive\" = true");
        builder.HasOne(x => x.BusinessCalendar).WithMany().HasForeignKey(x => x.BusinessCalendarId).OnDelete(DeleteBehavior.Restrict);
    }
}
