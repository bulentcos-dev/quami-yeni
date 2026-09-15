using Quami.Application.Abstractions;

namespace Quami.Infrastructure.Persistence;

/// <summary>
/// Tasarım zamanı (migration üretimi) ve arka plan işleri için boş bağlam.
/// Oturum yok: kiracı yok, kullanıcı yok, muafiyet yok. Üretimde istek
/// hattında KULLANILMAZ; oturumdan beslenen uygulama adım 6'da gelir.
/// </summary>
public sealed class DesignTimeTenantContext : ITenantContext
{
    public Guid? TenantId => null;
    public Guid? UserId => null;
    public bool IsSystemAdmin => false;
}
