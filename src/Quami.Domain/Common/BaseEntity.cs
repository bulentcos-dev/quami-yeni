namespace Quami.Domain.Common;

/// <summary>
/// Tüm varlıkların ortak tabanı. Kiracıya bağlı olmayan sistem geneli
/// varlıklar (Tenant, Module, MenuGroup) doğrudan bundan türer;
/// kiracıya ait olanlar <see cref="TenantEntity"/>'den türer.
/// </summary>
public abstract class BaseEntity : IAuditable, ISoftDeletable
{
    /// <summary>Sıralı GUID (v7): PostgreSQL indeksinde dağılmayı önler.</summary>
    public Guid Id { get; set; } = Guid.CreateVersion7();

    /// <summary>Eski Quami'deki kaydın kimliği. D-Day veri taşımasında eşleştirme için.</summary>
    public string? LegacyId { get; set; }

    public DateTime CreatedAt { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedByUserId { get; set; }

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public Guid? DeletedByUserId { get; set; }
}
