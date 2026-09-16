using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Quami.Application.Abstractions;

namespace Quami.Infrastructure.Identity;

/// <summary>
/// <see cref="ITenantContext"/>'in gerçek uygulaması: kiracı ve kullanıcı
/// bilgisini oturum biletindeki taleplerden (claim) okur.
///
/// Oturum sahibine iki ayrı yerden ulaşılır, çünkü Blazor Server'da ikisi de
/// gerekir:
/// 1. <see cref="IHttpContextAccessor"/> — istek hattı: statik render, form
///    gönderimi, minimal API uçları.
/// 2. <see cref="AuthenticationStateProvider"/> — etkileşimli devre (circuit).
///    Devre içinde HttpContext yoktur.
///
/// Bulunan değer kapsam (scope) boyunca önbelleğe alınır; bulunamazsa
/// önbelleğe alınmaz, bir sonraki erişimde yeniden denenir.
/// </summary>
public sealed class CurrentUserTenantContext(
    IHttpContextAccessor httpContextAccessor,
    IServiceProvider services) : ITenantContext
{
    private ClaimsPrincipal? _cached;

    public Guid? TenantId =>
        Guid.TryParse(FindClaim(QuamiClaimTypes.TenantId), out var id) ? id : null;

    public Guid? UserId =>
        Guid.TryParse(FindClaim(ClaimTypes.NameIdentifier), out var id) ? id : null;

    public bool IsSystemAdmin =>
        string.Equals(FindClaim(QuamiClaimTypes.IsSystemAdmin), "true", StringComparison.OrdinalIgnoreCase);

    private string? FindClaim(string claimType) => GetPrincipal()?.FindFirst(claimType)?.Value;

    private ClaimsPrincipal? GetPrincipal()
    {
        if (_cached is not null)
            return _cached;

        var fromHttp = httpContextAccessor.HttpContext?.User;
        if (fromHttp?.Identity?.IsAuthenticated == true)
            return _cached = fromHttp;

        var provider = services.GetService<AuthenticationStateProvider>();
        if (provider is null)
            return null;

        try
        {
            var task = provider.GetAuthenticationStateAsync();
            // Devre kurulurken durum henüz hazır olmayabilir; bloklamak yerine
            // boş dönülür, bir sonraki erişimde yeniden bakılır.
            if (!task.IsCompletedSuccessfully)
                return null;

            var user = task.Result.User;
            return user.Identity?.IsAuthenticated == true ? _cached = user : null;
        }
        catch (InvalidOperationException)
        {
            // Durum henüz atanmamış.
            return null;
        }
    }
}
