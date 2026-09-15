using Quami.Domain.Common;

namespace Quami.Domain.Entities;

/// <summary>
/// Lisanslanabilir iş modülü (TASKS, DOCS, AUDITMGMT ...). Sistem geneli tanım;
/// kiracıya açılışı <see cref="TenantModule"/> ile yapılır.
/// </summary>
public class Module : BaseEntity
{
    /// <summary>Sabit kod, ör. DOCS. Benzersiz; lisans kontrolü bu kodla yapılır.</summary>
    public string Code { get; set; } = string.Empty;

    public string NameTr { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;

    public Guid MenuGroupId { get; set; }
    public MenuGroup MenuGroup { get; set; } = null!;

    /// <summary>Grup içindeki sıra.</summary>
    public int SortOrder { get; set; }

    public string? Icon { get; set; }

    /// <summary>Menü bağlantısının adresi, ör. /docs.</summary>
    public string Route { get; set; } = string.Empty;

    /// <summary>
    /// Ürün olarak hazır mı? Hazır olmayan modül (ENV, FOOD) menüde "yakında"
    /// olarak görünür, kiracıya lisanslanamaz.
    /// </summary>
    public bool IsAvailable { get; set; } = true;

    public ICollection<TenantModule> TenantModules { get; set; } = new List<TenantModule>();
}
