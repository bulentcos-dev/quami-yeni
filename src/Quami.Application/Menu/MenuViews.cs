namespace Quami.Application.Menu;

/// <summary>Menüdeki tek bir modül satırı.</summary>
/// <param name="IsAvailable">
/// false ise modül ürün olarak hazır değildir: soluk görünür, tıklanmaz ("yakında").
/// </param>
public sealed record MenuItemView(
    string Code, string Name, string Route, string? Icon, bool IsAvailable);

/// <summary>
/// Menüdeki ana başlık. <paramref name="IsDirectLink"/> true ise açılır başlık
/// değil, doğrudan bağlantı olarak çizilir (Ana sayfa).
/// </summary>
public sealed record MenuGroupView(
    string Code, string Name, string? Icon, bool IsDirectLink,
    string? DirectRoute, IReadOnlyList<MenuItemView> Items);

/// <summary>Bir adresin açılıp açılamayacağı.</summary>
public enum ModuleAccess
{
    /// <summary>Adres bir modüle ait değil (giriş, hata sayfası vb.): serbest.</summary>
    NotAModule = 0,

    /// <summary>Modül bu kiracı için etkin: açılabilir.</summary>
    Allowed = 1,

    /// <summary>Modül var ama kiracının lisansı yok: açılamaz.</summary>
    NotLicensed = 2,

    /// <summary>Modül ürün olarak hazır değil ("yakında"): açılamaz.</summary>
    NotAvailable = 3
}

/// <summary>
/// Menüyü veriden kurar ve adres bazlı erişim kontrolü yapar.
/// Menüde koda gömülü tek satır yoktur; her şey MenuGroups/Modules
/// tablolarından ve kiracının lisanslarından gelir.
/// </summary>
public interface IMenuService
{
    /// <summary>Kiracının menüsü, sırayla. Boş grup döndürülmez.</summary>
    Task<IReadOnlyList<MenuGroupView>> GetMenuAsync(Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>Verilen adres bu kiracı için açılabilir mi?</summary>
    Task<ModuleAccess> CheckRouteAsync(Guid tenantId, string path, CancellationToken cancellationToken = default);
}
