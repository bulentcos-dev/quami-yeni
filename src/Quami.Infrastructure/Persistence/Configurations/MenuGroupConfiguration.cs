using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Quami.Domain.Entities;

namespace Quami.Infrastructure.Persistence.Configurations;

public class MenuGroupConfiguration : BaseEntityConfiguration<MenuGroup>
{
    public override void Configure(EntityTypeBuilder<MenuGroup> builder)
    {
        base.Configure(builder);
        builder.Property(e => e.Code).HasMaxLength(30).IsRequired();
        builder.Property(e => e.NameTr).HasMaxLength(100).IsRequired();
        builder.Property(e => e.NameEn).HasMaxLength(100).IsRequired();
        builder.Property(e => e.Icon).HasMaxLength(50);
        HasActiveUniqueIndex(builder, e => e.Code);
    }
}
