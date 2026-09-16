using Quami.Application.Abstractions;

namespace Quami.Infrastructure.Persistence;

/// <summary>
/// Boş bağlam: kiracı yok, kullanıcı yok, muafiyet yok.
/// Kullanıldığı yerler: migration üretimi (tasarım zamanı) ve açılıştaki seed.
/// İstek hattında geçici olarak kayıtlıdır; oturumdan beslenen gerçek uygulama
/// adım 6'da bunun yerini alır.
/// </summary>
public sealed class NullTenantContext : ITenantContext
{
    public Guid? TenantId => null;
    public Guid? UserId => null;
    public bool IsSystemAdmin => false;
}
