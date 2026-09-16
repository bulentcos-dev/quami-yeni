using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Quami.Application.Abstractions;
using Quami.Infrastructure.Identity;
using Quami.Infrastructure.Persistence;
using Quami.Infrastructure.Services;

namespace Quami.Infrastructure;

public static class DependencyInjection
{
    public const string ConnectionStringName = "QuamiDb";

    /// <summary>
    /// Veri katmanı ve ortak servisler. Web ve Api projeleri Program.cs'te çağırır.
    /// Bağlantı dizesi: ConnectionStrings:QuamiDb.
    /// </summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(ConnectionStringName)
            ?? throw new InvalidOperationException($"ConnectionStrings:{ConnectionStringName} tanımlı değil.");

        services.AddDbContext<QuamiDbContext>(options =>
            options
                .UseNpgsql(connectionString)
                .UseSnakeCaseNamingConvention());

        // Oturum yoksa (migration, arka plan işleri, seed) kullanılan boş bağlam.
        // AddQuamiIdentity çağrılırsa gerçek uygulama bunun yerini alır.
        services.TryAddScoped<ITenantContext, NullTenantContext>();

        services.TryAddSingleton(TimeProvider.System);
        services.AddScoped<IModuleLicenseService, ModuleLicenseService>();

        return services;
    }

    /// <summary>
    /// Kimlik, oturum çerezi ve oturumdan beslenen kiracı bağlamı.
    /// Bugün yalnızca yerel parola doğrulaması kayıtlı; SAML / Entra ID
    /// ileride birer <see cref="IAuthenticationProvider"/> olarak eklenecek.
    /// </summary>
    public static IServiceCollection AddQuamiIdentity(this IServiceCollection services)
    {
        services.AddIdentityCore<QuamiIdentityUser>(options =>
        {
            options.Password.RequiredLength = 10;
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireNonAlphanumeric = true;

            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);

            // Giriş adı "kullanıcıadı@KURUMKODU"; e-posta yalnızca kiracı
            // içinde benzersiz olduğu için Identity'de benzersizlik aranmaz.
            options.User.RequireUniqueEmail = false;
            options.SignIn.RequireConfirmedAccount = false;
        })
        .AddRoles<QuamiIdentityRole>()
        .AddEntityFrameworkStores<QuamiDbContext>()
        .AddClaimsPrincipalFactory<QuamiUserClaimsPrincipalFactory>()
        .AddSignInManager()
        .AddDefaultTokenProviders();

        services.AddAuthentication(IdentityConstants.ApplicationScheme)
            .AddIdentityCookies();

        services.ConfigureApplicationCookie(options =>
        {
            options.Cookie.Name = "quami.auth";
            options.Cookie.HttpOnly = true;
            options.Cookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Lax;
            options.ExpireTimeSpan = TimeSpan.FromHours(8);
            options.SlidingExpiration = true;
            options.LoginPath = "/login";
            // Giriş ekranının beklediği ad ile aynı olsun (varsayılan "ReturnUrl").
            options.ReturnUrlParameter = "returnUrl";
            options.LogoutPath = "/logout";
            options.AccessDeniedPath = "/login";
        });

        services.AddAuthorization();
        services.AddHttpContextAccessor();

        services.AddScoped<IAuthEventLog, AuthEventLog>();
        services.AddScoped<IAuthenticationProvider, LocalAuthenticationProvider>();

        // Boş bağlam yerine oturumdan beslenen gerçek uygulama.
        services.RemoveAll<ITenantContext>();
        services.AddScoped<ITenantContext, CurrentUserTenantContext>();

        return services;
    }
}
