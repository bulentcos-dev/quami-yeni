using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Quami.Domain.Entities;

namespace Quami.Infrastructure.Persistence.Configurations;

public class TenantModuleConfiguration : BaseEntityConfiguration<TenantModule>
{
    public override void Configure(EntityTypeBuilder<TenantModule> builder)
    {
        base.Configure(builder);
        HasActiveUniqueIndex(builder, e => new { e.TenantId, e.ModuleId });

        builder.HasOne(e => e.Tenant)
            .WithMany(t => t.TenantModules)
            .HasForeignKey(e => e.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Module)
            .WithMany(m => m.TenantModules)
            .HasForeignKey(e => e.ModuleId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
