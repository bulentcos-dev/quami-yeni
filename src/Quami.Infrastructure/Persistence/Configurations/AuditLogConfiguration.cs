using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Quami.Domain.Entities;

namespace Quami.Infrastructure.Persistence.Configurations;

/// <summary>Salt ekleme tablo; taban yapılandırmadan türemez.</summary>
public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).UseIdentityByDefaultColumn();
        builder.Property(e => e.TableName).HasMaxLength(100).IsRequired();
        builder.Property(e => e.RecordId).HasMaxLength(200).IsRequired();
        builder.Property(e => e.Operation).HasConversion<string>().HasMaxLength(10);
        builder.Property(e => e.OldValues).HasColumnType("jsonb");
        builder.Property(e => e.NewValues).HasColumnType("jsonb");

        builder.HasIndex(e => new { e.TableName, e.RecordId });
        builder.HasIndex(e => new { e.TenantId, e.Timestamp });
    }
}
