using Quami.Domain.Enums;

namespace Quami.Domain.Entities;

/// <summary>
/// Kimlik olay günlüğü: giriş, çıkış, başarısız deneme, kilitlenme, parola
/// değişikliği. Denetim günlüğünden (<see cref="AuditLog"/>) BAĞIMSIZ ayrı bir
/// tablodur; denetim günlüğü veri değişikliklerini, bu ise kimlik olaylarını
/// izler. Salt ekleme: güncellenmez, silinmez, mantıksal silme yoktur.
/// ISO 27001 denetiminde istenen ilk kayıtlardandır.
///
/// PAROLA VEYA PAROLA ÖZETİ BU TABLOYA HİÇBİR ŞEKİLDE YAZILMAZ.
/// </summary>
public class AuthEvent
{
    public long Id { get; set; }

    /// <summary>Olay zamanı, UTC.</summary>
    public DateTime Timestamp { get; set; }

    public AuthEventType EventType { get; set; }

    /// <summary>Başarısızlık sebebi. Yalnızca günlükte tutulur, ekrana yansımaz.</summary>
    public AuthFailureReason? FailureReason { get; set; }

    /// <summary>Girişte yazılan kurum kodu. Yanlış olabilir; olduğu gibi saklanır.</summary>
    public string? TenantShortName { get; set; }

    /// <summary>Girişte yazılan kullanıcı adı. Yanlış olabilir; olduğu gibi saklanır.</summary>
    public string? AttemptedUserName { get; set; }

    /// <summary>Kurum bulunabildiyse kimliği.</summary>
    public Guid? TenantId { get; set; }

    /// <summary>Kullanıcı bulunabildiyse kimliği.</summary>
    public Guid? UserId { get; set; }

    /// <summary>İstemci adresi. Ters vekil arkasındaysa iletilen başlıklar yapılandırılmalıdır.</summary>
    public string? IpAddress { get; set; }

    /// <summary>Tarayıcı bilgisi (user agent).</summary>
    public string? UserAgent { get; set; }
}
