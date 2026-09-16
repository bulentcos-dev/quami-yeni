using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Quami.Application.Abstractions;
using Quami.Domain.Enums;
using Quami.Infrastructure.Persistence;

namespace Quami.Web.Localization;

public static class CultureEndpoints
{
    /// <summary>Desteklenen diller. Varsayılan ilkidir.</summary>
    public static readonly string[] SupportedCultures = ["tr", "en"];

    /// <summary>
    /// Dil değiştirme ucu. Seçimi çereze yazar, oturum açıksa kullanıcının
    /// kaydına da işler, sonra gelinen sayfaya döner. Tam sayfa yüklemesi
    /// gerekir: kültür, Blazor devresi kurulurken belirlenir.
    /// </summary>
    public static void MapCultureEndpoint(this WebApplication app)
    {
        app.MapGet("/culture", async (
            string? lang,
            string? returnUrl,
            HttpContext http,
            ITenantContext tenantContext,
            QuamiDbContext db) =>
        {
            var language = SupportedCultures.Contains(lang, StringComparer.OrdinalIgnoreCase)
                ? lang!.ToLowerInvariant()
                : SupportedCultures[0];

            http.Response.Cookies.Append(
                CookieRequestCultureProvider.DefaultCookieName,
                CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(language)),
                new CookieOptions
                {
                    Expires = DateTimeOffset.UtcNow.AddYears(1),
                    IsEssential = true,
                    HttpOnly = true,
                    SameSite = SameSiteMode.Lax
                });

            if (tenantContext.UserId is { } userId)
            {
                var user = await db.BusinessUsers
                    .IgnoreQueryFilters([QuamiDbContext.TenantFilter])
                    .FirstOrDefaultAsync(u => u.Id == userId);

                if (user is not null)
                {
                    user.Language = language == "en" ? Language.En : Language.Tr;
                    await db.SaveChangesAsync();
                }
            }

            return Results.Redirect(SafeLocalPath(returnUrl));
        });
    }

    /// <summary>Açık yönlendirme koruması: yalnızca site içi adresler.</summary>
    private static string SafeLocalPath(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return "/";
        if (!url.StartsWith('/') || url.StartsWith("//") || url.StartsWith("/\\")) return "/";
        return url;
    }
}
