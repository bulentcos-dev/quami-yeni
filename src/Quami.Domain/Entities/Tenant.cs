using Quami.Domain.Common;

namespace Quami.Domain.Entities;

/// <summary>Kiracı (müşteri kurum). Sistem geneli varlık, kiracıya bağlı değildir.</summary>
public class Tenant : BaseEntity
{
    /// <summary>Kurumun tam adı.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Kısa ad / kod. Benzersiz; alt alan adı ve raporlarda kullanılır.</summary>
    public string ShortName { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public ICollection<TenantModule> TenantModules { get; set; } = new List<TenantModule>();
    public ICollection<User> Users { get; set; } = new List<User>();
}
