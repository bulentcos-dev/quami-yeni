using Quami.Domain.Common;

namespace Quami.Domain.Entities;

/// <summary>
/// Sol menüdeki ana başlık (HOME, MYWORK, DOCREC, AUDIT, RISK, SYSTEM).
/// Menü koda gömülmez, buradan okunur.
/// </summary>
public class MenuGroup : BaseEntity
{
    /// <summary>Sabit kod, ör. MYWORK. Benzersiz.</summary>
    public string Code { get; set; } = string.Empty;

    public string NameTr { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;

    public int SortOrder { get; set; }

    /// <summary>
    /// True ise menüde açılır başlık değil, doğrudan bağlantı olarak çizilir
    /// (Ana sayfa böyledir). Tek modülü varsa o modülün adresine gider.
    /// Menünün davranışı koda değil, bu veriye bağlıdır.
    /// </summary>
    public bool IsDirectLink { get; set; }

    /// <summary>İkon adı (tema ikon setinden).</summary>
    public string? Icon { get; set; }

    public ICollection<Module> Modules { get; set; } = new List<Module>();
}
