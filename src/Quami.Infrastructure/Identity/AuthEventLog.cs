using Microsoft.AspNetCore.Http;
using Quami.Application.Abstractions;
using Quami.Domain.Entities;
using Quami.Infrastructure.Persistence;

namespace Quami.Infrastructure.Identity;

/// <summary>
/// Kimlik olaylarını <c>auth_events</c> tablosuna yazar. IP adresi ve tarayıcı
/// bilgisi istekten okunur. Parola hiçbir aşamada bu sınıfa geçmez.
/// </summary>
public sealed class AuthEventLog(
    QuamiDbContext db,
    IHttpContextAccessor httpContextAccessor,
    TimeProvider timeProvider) : IAuthEventLog
{
    private const int UserAgentMaxLength = 512;

    public async Task RecordAsync(AuthEventEntry entry, CancellationToken cancellationToken = default)
    {
        var http = httpContextAccessor.HttpContext;

        var userAgent = http?.Request.Headers.UserAgent.ToString();
        if (userAgent is { Length: > UserAgentMaxLength })
            userAgent = userAgent[..UserAgentMaxLength];

        db.AuthEvents.Add(new AuthEvent
        {
            Timestamp = timeProvider.GetUtcNow().UtcDateTime,
            EventType = entry.EventType,
            FailureReason = entry.FailureReason,
            TenantShortName = Trim(entry.TenantShortName, 50),
            AttemptedUserName = Trim(entry.AttemptedUserName, 100),
            TenantId = entry.TenantId,
            UserId = entry.UserId,
            IpAddress = http?.Connection.RemoteIpAddress?.ToString(),
            UserAgent = string.IsNullOrWhiteSpace(userAgent) ? null : userAgent
        });

        await db.SaveChangesAsync(cancellationToken);
    }

    private static string? Trim(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        value = value.Trim();
        return value.Length > maxLength ? value[..maxLength] : value;
    }
}
