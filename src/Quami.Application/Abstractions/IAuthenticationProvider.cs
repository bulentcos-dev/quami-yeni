namespace Quami.Application.Abstractions;

/// <summary>Giriş denemesinin sonucu.</summary>
public enum SignInStatus
{
    Success = 0,
    /// <summary>Kurum kodu, kullanıcı adı veya parola hatalı. Hangisi olduğu söylenmez.</summary>
    InvalidCredentials = 1,
    /// <summary>Art arda hatalı deneme nedeniyle hesap geçici olarak kilitli.</summary>
    LockedOut = 2,
    /// <summary>Kimlik doğru ama hesap veya kurum pasif.</summary>
    NotAllowed = 3,
    /// <summary>İki adımlı doğrulama gerekiyor (bu fazda kullanılmıyor).</summary>
    RequiresTwoFactor = 4
}

/// <summary>
/// Kimlik doğrulama sağlayıcısı. Bugün yerel kullanıcı adı/parola
/// (<c>LocalAuthenticationProvider</c>); ileride SAML ve Entra ID birer ikinci
/// uygulama olarak eklenecek. Giriş ekranı bu arayüzden başkasını bilmez.
/// </summary>
public interface IAuthenticationProvider
{
    /// <summary>Sağlayıcının adı, ör. "Local".</summary>
    string Scheme { get; }

    /// <summary>Kullanıcı adı/parola ile giriş destekleniyor mu? SSO sağlayıcılarında false olur.</summary>
    bool SupportsPasswordSignIn { get; }

    /// <summary>Kurum kodu + kullanıcı adı + parola ile oturum açar.</summary>
    Task<SignInStatus> SignInWithPasswordAsync(
        string tenantShortName, string userName, string password, bool rememberMe,
        CancellationToken cancellationToken = default);

    /// <summary>Oturumu kapatır.</summary>
    Task SignOutAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Kullanıcının parolasını değiştirir. Başarılı da olsa olmasa da kimlik
    /// olay günlüğüne yazılır. Parola yönetimi ekranı bu yöntemi çağırır.
    /// </summary>
    Task<bool> ChangePasswordAsync(
        Guid userId, string currentPassword, string newPassword,
        CancellationToken cancellationToken = default);
}
