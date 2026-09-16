using Microsoft.AspNetCore.Identity;

namespace Quami.Infrastructure.Identity;

/// <summary>
/// Kimlik doğrulama kaydı: parola özeti, kilitlenme, iki adımlı doğrulama.
/// İş bilgileri Domain'deki User'da durur; ikisi AYNI Id'yi paylaşır.
///
/// Identity'nin kullanıcı adı genelde benzersiz olmak zorundadır, bizim
/// Domain kullanıcı adımız ise yalnızca kiracı içinde benzersizdir. Bu yüzden
/// Identity tarafında kullanıcı adı "kullanıcıadı@KURUMKODU" olarak birleştirilir
/// (bkz. <see cref="ComposeUserName"/>).
/// </summary>
public class QuamiIdentityUser : IdentityUser<Guid>
{
    /// <summary>Identity için benzersiz kullanıcı adı üretir: "admin@DEMO".</summary>
    public static string ComposeUserName(string tenantShortName, string userName) =>
        $"{userName}@{tenantShortName}";
}
