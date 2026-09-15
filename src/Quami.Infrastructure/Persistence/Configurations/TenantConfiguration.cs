using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Quami.Domain.Entities;

namespace Quami.Infrastructure.Persistence.Configurations;

public class TenantConfiguration : BaseEntityConfiguration<Tenant>
{
    public override void Configure(EntityTypeBuilder<Tenant> builder)
    {
        base.Configure(builder);
        builder.Property(e => e.Name).HasMaxLength(200).IsRequired();
        builder.Property(e => e.ShortName).HasMaxLength(50).IsRequired();
        HasActiveUniqueIndex(builder, e => e.ShortName);
    }
}
