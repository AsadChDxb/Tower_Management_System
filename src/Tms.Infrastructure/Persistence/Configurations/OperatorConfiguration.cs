using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tms.Domain.Entities;

namespace Tms.Infrastructure.Persistence.Configurations;

public class OperatorConfiguration : IEntityTypeConfiguration<Operator>
{
    public void Configure(EntityTypeBuilder<Operator> builder)
    {
        builder.ToTable("Operators");

        builder.HasKey(o => o.Id);

        builder.Property(o => o.Name).IsRequired().HasMaxLength(120);
        builder.Property(o => o.CountryCode).IsRequired().HasMaxLength(2);
        builder.Property(o => o.ContactEmail).HasMaxLength(200);
        builder.Property(o => o.ContactPhone).HasMaxLength(40);

        builder.HasIndex(o => o.Name).IsUnique().HasDatabaseName("UX_Operators_Name");
    }
}
