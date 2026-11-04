using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tms.Domain.Entities;

namespace Tms.Infrastructure.Persistence.Configurations;

public class SiteConfiguration : IEntityTypeConfiguration<Site>
{
    public void Configure(EntityTypeBuilder<Site> builder)
    {
        builder.ToTable("Sites");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.SiteCode).IsRequired().HasMaxLength(32);
        builder.Property(s => s.Name).IsRequired().HasMaxLength(120);
        builder.Property(s => s.CountryCode).IsRequired().HasMaxLength(2);
        builder.Property(s => s.City).IsRequired().HasMaxLength(80);
        builder.Property(s => s.Address).IsRequired().HasMaxLength(200);
        builder.Property(s => s.Notes).HasMaxLength(1000);
        builder.Property(s => s.MonthlySiteCost).HasPrecision(18, 2);

        builder.Property(s => s.SiteType).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(s => s.Status).HasConversion<string>().HasMaxLength(20).IsRequired();

        builder.HasIndex(s => s.SiteCode).IsUnique().HasDatabaseName("UX_Sites_SiteCode");
        builder.HasIndex(s => s.CountryCode).HasDatabaseName("IX_Sites_CountryCode");
        builder.HasIndex(s => s.Status).HasDatabaseName("IX_Sites_Status");

        builder.HasMany(s => s.Towers)
            .WithOne(t => t.Site)
            .HasForeignKey(t => t.SiteId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
