namespace Quami.Domain.Common;

/// <summary>
/// Kiracıya ait varlık. Global query filter TenantId'ye göre süzer,
/// SaveChanges override'ı TenantId'yi ITenantContext'ten otomatik doldurur.
/// </summary>
public interface ITenantScoped
{
    Guid TenantId { get; set; }
}
