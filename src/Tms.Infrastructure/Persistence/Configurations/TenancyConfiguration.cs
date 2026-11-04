using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tms.Domain.Entities;

namespace Tms.Infrastructure.Persistence.Configurations;

public class TenancyConfiguration : IEntityTypeConfiguration<Tenancy>
{
    public void Configure(EntityTypeBuilder<Tenancy> builder)
    {
        builder.ToTable("Tenancies");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.MonthlyRent).HasPrecision(18, 2);
        builder.Property(t => t.Currency).IsRequired().HasMaxLength(3);
        builder.Property(t => t.Notes).HasMaxLength(500);
        builder.Property(t => t.SpaceType).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(t => t.Status).HasConversion<string>().HasMaxLength(20).IsRequired();

        builder.HasIndex(t => t.TowerId).HasDatabaseName("IX_Tenancies_TowerId");
        builder.HasIndex(t => t.OperatorId).HasDatabaseName("IX_Tenancies_OperatorId");
        builder.HasIndex(t => t.Status).HasDatabaseName("IX_Tenancies_Status");

        // Business rule at the database level as well: an operator can hold only one
        // active tenancy per tower (filtered unique index).
        builder.HasIndex(t => new { t.TowerId, t.OperatorId })
            .IsUnique()
            .HasFilter("[Status] = 'Active'")
            .HasDatabaseName("UX_Tenancies_ActiveOperatorPerTower");

        builder.HasOne(t => t.Tower)
            .WithMany(x => x.Tenancies)
            .HasForeignKey(t => t.TowerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.Operator)
            .WithMany(o => o.Tenancies)
            .HasForeignKey(t => t.OperatorId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
