namespace Quami.Domain.Enums;

/// <summary>Kimlik olay günlüğüne yazılan olay türü.</summary>
public enum AuthEventType
{
    /// <summary>Başarılı giriş.</summary>
    SignInSucceeded = 1,

    /// <summary>Başarısız giriş denemesi. Sebep <see cref="AuthFailureReason"/> alanındadır.</summary>
    SignInFailed = 2,

    /// <summary>Oturum kapatma.</summary>
    SignedOut = 3,

    /// <summary>Art arda hatalı deneme sonucu hesabın kilitlenmesi.</summary>
    AccountLockedOut = 4,

    /// <summary>Parola değişikliği.</summary>
    PasswordChanged = 5
}
