using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Quami.Domain.Entities;

namespace Quami.Infrastructure.Persistence.Configurations;

public class ModuleConfiguration : BaseEntityConfiguration<Module>
{
    public override void Configure(EntityTypeBuilder<Module> builder)
    {
        base.Configure(builder);
        builder.Property(e => e.Code).HasMaxLength(30).IsRequired();
        builder.Property(e => e.NameTr).HasMaxLength(100).IsRequired();
        builder.Property(e => e.NameEn).HasMaxLength(100).IsRequired();
        builder.Property(e => e.Icon).HasMaxLength(50);
        builder.Property(e => e.Route).HasMaxLength(200).IsRequired();
        HasActiveUniqueIndex(builder, e => e.Code);

        builder.HasOne(e => e.MenuGroup)
            .WithMany(g => g.Modules)
            .HasForeignKey(e => e.MenuGroupId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
