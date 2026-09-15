using Quami.Domain.Common;

namespace Quami.Domain.Entities;

/// <summary>
/// Bir modülün bir kiracıya lisanslanması. Kural: aktif değilse veya tarih
/// aralığı dışındaysa modül menüde görünmez ve adresle de açılamaz.
/// (TenantId, ModuleId) benzersizdir.
/// </summary>
public class TenantModule : TenantEntity
{
    public Tenant Tenant { get; set; } = null!;

    public Guid ModuleId { get; set; }
    public Module Module { get; set; } = null!;

    public bool IsActive { get; set; } = true;

    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }

    /// <summary>Verilen anda lisans geçerli mi? IModuleLicenseService bu kuralı kullanır.</summary>
    public bool IsEffectiveAt(DateTime utcNow) =>
        IsActive
        && (StartDate is null || StartDate <= utcNow)
        && (EndDate is null || EndDate > utcNow);
}
