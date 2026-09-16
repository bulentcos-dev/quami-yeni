using Microsoft.AspNetCore.Localization;
using Quami.Application.Abstractions;

namespace Quami.Web.Localization;

/// <summary>
/// Kullanıcının kayıtlı dil tercihini oturum biletindeki talepten okur.
/// Çerez sağlayıcısından SONRA çalışır: tarayıcıda yapılmış açık seçim
/// kullanıcının kayıtlı tercihini geçersiz kılar. Yeni bir tarayıcıda
/// çerez yokken kullanıcının kendi dili gelir.
/// </summary>
public sealed class UserClaimCultureProvider : RequestCultureProvider
{
    public override Task<ProviderCultureResult?> DetermineProviderCultureResult(HttpContext httpContext)
    {
        var language = httpContext.User?.FindFirst(QuamiClaimTypes.Language)?.Value;

        if (string.IsNullOrWhiteSpace(language))
            return Task.FromResult<ProviderCultureResult?>(null);

        return Task.FromResult<ProviderCultureResult?>(new ProviderCultureResult(language, language));
    }
}
