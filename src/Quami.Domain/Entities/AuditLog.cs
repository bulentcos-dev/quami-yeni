using Quami.Domain.Enums;

namespace Quami.Domain.Entities;

/// <summary>
/// Denetim günlüğü. Her ekleme / güncelleme / silme SaveChanges override'ı
/// tarafından otomatik yazılır. Salt ekleme (append-only) tablo: BaseEntity'den
/// türemez, çünkü kendisi denetlenmez ve silinmez. Faz 2'deki öneri motoru
/// bu geçmişten beslenir.
/// </summary>
public class AuditLog
{
    public long Id { get; set; }

    /// <summary>Sistem geneli varlıklarda (Tenant, Module) null olabilir.</summary>
    public Guid? TenantId { get; set; }

    public Guid? UserId { get; set; }

    public string TableName { get; set; } = string.Empty;

    /// <summary>Etkilenen kaydın kimliği (Guid veya bileşik anahtar metni).</summary>
    public string RecordId { get; set; } = string.Empty;

    public AuditOperation Operation { get; set; }

    /// <summary>Değişiklik öncesi değerler, JSON (jsonb). Insert'te null.</summary>
    public string? OldValues { get; set; }

    /// <summary>Değişiklik sonrası değerler, JSON (jsonb). Delete'te null.</summary>
    public string? NewValues { get; set; }

    public DateTime Timestamp { get; set; }
}
