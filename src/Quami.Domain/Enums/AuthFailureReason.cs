namespace Quami.Domain.Enums;

/// <summary>
/// Başarısız girişin gerçek sebebi. YALNIZCA günlüğe yazılır; ekrana dönen
/// mesaj her durumda aynıdır, yoksa kullanıcı adı taraması yapılabilir.
/// </summary>
public enum AuthFailureReason
{
    /// <summary>Kurum kodu hiçbir kuruma karşılık gelmiyor.</summary>
    TenantNotFound = 1,

    /// <summary>Kurum var ama pasif.</summary>
    TenantInactive = 2,

    /// <summary>Kurumda bu kullanıcı adı yok.</summary>
    UserNotFound = 3,

    /// <summary>Kullanıcı var ama pasif.</summary>
    UserInactive = 4,

    /// <summary>Parola yanlış.</summary>
    InvalidPassword = 5,

    /// <summary>Hesap kilitli olduğu için deneme reddedildi.</summary>
    LockedOut = 6,

    /// <summary>İki adımlı doğrulama gerekiyor.</summary>
    RequiresTwoFactor = 7,

    /// <summary>Parola değiştirilemedi (eski parola yanlış veya kural dışı).</summary>
    PasswordChangeRejected = 8
}
