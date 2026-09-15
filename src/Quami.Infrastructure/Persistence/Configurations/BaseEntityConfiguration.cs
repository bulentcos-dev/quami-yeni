using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Quami.Domain.Common;

namespace Quami.Infrastructure.Persistence.Configurations;

/// <summary>
/// Taban sınıf alanlarının ortak eşlemesi. Her varlık yapılandırması bundan türer.
/// Benzersiz indeksler her zaman <see cref="HasActiveUniqueIndex"/> ile kısmi
/// (is_deleted = false) tanımlanır ki silinmiş kayıt yeni kaydı engellemesin.
/// </summary>
public abstract class BaseEntityConfiguration<TEntity> : IEntityTypeConfiguration<TEntity>
    where TEntity : BaseEntity
{
    public const string NotDeletedFilter = "is_deleted = false";

    public virtual void Configure(EntityTypeBuilder<TEntity> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();
        builder.Property(e => e.LegacyId).HasMaxLength(100);
        builder.Property(e => e.IsDeleted).HasDefaultValue(false);
        builder.HasIndex(e => e.LegacyId);
    }

    /// <summary>Yalnızca silinmemiş satırları kapsayan benzersiz indeks.</summary>
    protected static void HasActiveUniqueIndex(EntityTypeBuilder<TEntity> builder,
        System.Linq.Expressions.Expression<Func<TEntity, object?>> keys)
    {
        builder.HasIndex(keys).IsUnique().HasFilter(NotDeletedFilter);
    }
}
