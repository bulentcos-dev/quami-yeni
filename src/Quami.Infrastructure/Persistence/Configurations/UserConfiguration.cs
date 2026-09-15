using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Quami.Domain.Entities;

namespace Quami.Infrastructure.Persistence.Configurations;

public class UserConfiguration : BaseEntityConfiguration<User>
{
    public override void Configure(EntityTypeBuilder<User> builder)
    {
        base.Configure(builder);
        builder.Property(e => e.UserName).HasMaxLength(100).IsRequired();
        builder.Property(e => e.Email).HasMaxLength(256).IsRequired();
        builder.Property(e => e.FullName).HasMaxLength(200).IsRequired();
        builder.Property(e => e.Language).HasConversion<string>().HasMaxLength(5);

        // Kiracı içinde benzersiz; silinmiş kullanıcı adı yeniden kullanılabilir.
        HasActiveUniqueIndex(builder, e => new { e.TenantId, e.UserName });
        HasActiveUniqueIndex(builder, e => new { e.TenantId, e.Email });

        builder.HasOne(e => e.Tenant)
            .WithMany(t => t.Users)
            .HasForeignKey(e => e.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
