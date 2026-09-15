namespace Quami.Application.Abstractions;

/// <summary>
/// Geçerli isteğin kiracı ve kullanıcı bağlamı. Scoped servis.
/// TenantId erişimi yalnızca buradan geçer; kiracı ayrım stratejisi (satır /
/// şema / veritabanı) değişirse tek değişecek yer burasıdır.
/// Adım 6'da oturumdan beslenen uygulaması gelir.
/// </summary>
public interface ITenantContext
{
    /// <summary>Geçerli kiracı. Oturum yoksa veya sistem yöneticisi kiracı seçmemişse null.</summary>
    Guid? TenantId { get; }

    /// <summary>Geçerli kullanıcı. Oturum yoksa null.</summary>
    Guid? UserId { get; }

    /// <summary>
    /// Sistem yöneticisi (ürün sahibi / destek): kiracı süzgecinden muaf, tüm
    /// kiracıları görür. Kaynağı User.IsSystemAdmin, uygulanışı burada.
    /// </summary>
    bool IsSystemAdmin { get; }
}
