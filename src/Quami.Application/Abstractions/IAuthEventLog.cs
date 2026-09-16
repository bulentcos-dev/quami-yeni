using Quami.Domain.Enums;

namespace Quami.Application.Abstractions;

/// <summary>
/// Günlüğe yazılacak kimlik olayı. IP adresi ve tarayıcı bilgisi çağıranın
/// sorumluluğunda değildir; uygulama isteğin kendisinden doldurur.
/// </summary>
public sealed record AuthEventEntry
{
    public required AuthEventType EventType { get; init; }

    /// <summary>Yalnızca başarısız olaylarda dolar.</summary>
    public AuthFailureReason? FailureReason { get; init; }

    public string? TenantShortName { get; init; }
    public string? AttemptedUserName { get; init; }
    public Guid? TenantId { get; init; }
    public Guid? UserId { get; init; }
}

/// <summary>
/// Kimlik olay günlüğü. Salt ekleme; kayıt güncellenmez, silinmez.
/// Parola veya parola özeti asla geçirilmez.
/// </summary>
public interface IAuthEventLog
{
    Task RecordAsync(AuthEventEntry entry, CancellationToken cancellationToken = default);
}
