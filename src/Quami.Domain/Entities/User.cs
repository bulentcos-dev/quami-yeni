using Quami.Domain.Common;
using Quami.Domain.Enums;

namespace Quami.Domain.Entities;

/// <summary>
/// Uygulama kullanıcısı (iş bilgileri). Parola, kilit, 2FA gibi kimlik
/// doğrulama verisi Domain'de tutulmaz; adım 6'da Infrastructure'daki
/// ASP.NET Core Identity kullanıcısı bu kayıtla aynı Id üzerinden eşlenir.
/// </summary>
public class User : TenantEntity
{
    public Tenant Tenant { get; set; } = null!;

    /// <summary>Giriş adı. Kiracı içinde benzersiz.</summary>
    public string UserName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Ürün sahibi / destek kullanıcısı. Kiracı süzgecinden muaftır, tüm kiracıları
    /// görür. Muafiyet ITenantContext üzerinden uygulanır, bu sınıf sadece işareti taşır.
    /// </summary>
    public bool IsSystemAdmin { get; set; }

    /// <summary>Arayüz dili tercihi.</summary>
    public Language Language { get; set; } = Language.Tr;

    public DateTime? LastLoginAt { get; set; }
}
