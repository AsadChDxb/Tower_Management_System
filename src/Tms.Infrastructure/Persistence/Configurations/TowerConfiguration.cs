using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tms.Domain.Entities;

namespace Tms.Infrastructure.Persistence.Configurations;

public class TowerConfiguration : IEntityTypeConfiguration<Tower>
{
    public void Configure(EntityTypeBuilder<Tower> builder)
    {
        builder.ToTable("Towers", table =>
            table.HasCheckConstraint("CK_Towers_TenantCapacity", "[TenantCapacity] >= 1"));

        builder.HasKey(t => t.Id);

        builder.Property(t => t.TowerCode).IsRequired().HasMaxLength(40);
        builder.Property(t => t.StructureType).HasConversion<string>().HasMaxLength(20).IsRequired();

        builder.HasIndex(t => t.TowerCode).IsUnique().HasDatabaseName("UX_Towers_TowerCode");
        builder.HasIndex(t => t.SiteId).HasDatabaseName("IX_Towers_SiteId");

        builder.HasOne(t => t.Site)
            .WithMany(s => s.Towers)
            .HasForeignKey(t => t.SiteId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(t => t.Tenancies)
            .WithOne(x => x.Tower)
            .HasForeignKey(x => x.TowerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
