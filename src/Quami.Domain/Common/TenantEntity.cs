namespace Quami.Domain.Common;

/// <summary>
/// Kiracıya ait varlıkların tabanı. İş modüllerinin tüm varlıkları buradan türer.
/// </summary>
public abstract class TenantEntity : BaseEntity, ITenantScoped
{
    public Guid TenantId { get; set; }
}
