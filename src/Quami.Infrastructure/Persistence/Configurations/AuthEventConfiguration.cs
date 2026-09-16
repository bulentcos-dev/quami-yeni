using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Quami.Domain.Entities;

namespace Quami.Infrastructure.Persistence.Configurations;

/// <summary>Salt ekleme tablo; taban yapılandırmadan türemez, süzgeç uygulanmaz.</summary>
public class AuthEventConfiguration : IEntityTypeConfiguration<AuthEvent>
{
    public void Configure(EntityTypeBuilder<AuthEvent> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).UseIdentityByDefaultColumn();

        builder.Property(e => e.EventType).HasConversion<string>().HasMaxLength(30);
        builder.Property(e => e.FailureReason).HasConversion<string>().HasMaxLength(30);
        builder.Property(e => e.TenantShortName).HasMaxLength(50);
        builder.Property(e => e.AttemptedUserName).HasMaxLength(100);
        builder.Property(e => e.IpAddress).HasMaxLength(45);   // IPv6 dahil
        builder.Property(e => e.UserAgent).HasMaxLength(512);

        builder.HasIndex(e => e.Timestamp);
        builder.HasIndex(e => new { e.TenantId, e.Timestamp });
        // Aynı kullanıcı adına yapılan deneme yığınlarını incelemek için.
        builder.HasIndex(e => new { e.AttemptedUserName, e.Timestamp });
    }
}
