using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Quami.Application.Abstractions;
using Quami.Application.Menu;
using Quami.Infrastructure.Identity;
using Quami.Infrastructure.Persistence;
using Quami.Infrastructure.Services;
using Quami.Infrastructure.Storage;

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

        // Blazor Server'da bir devre (circuit) içinde birden çok bileşen aynı anda
        // sorgu çalıştırabilir; tek bir paylaşılan DbContext bunu kaldırmaz
        // ("A second operation was started..."). Bu yüzden FABRİKA kullanılır:
        // okuma servisleri her çağrıda kendi kısa ömürlü bağlamını açar.
        // Fabrika kapsamlıdır (scoped), çünkü DbContext kapsamlı ITenantContext ister.
        services.AddDbContextFactory<QuamiDbContext>(
            (_, options) => options
                .UseNpgsql(connectionString)
                .UseSnakeCaseNamingConvention(),
            lifetime: ServiceLifetime.Scoped);

        // Tek iş parçacıklı akışlar (giriş, seed, dil ucu) doğrudan bağlam ister.
        services.AddScoped<QuamiDbContext>(sp =>
            sp.GetRequiredService<IDbContextFactory<QuamiDbContext>>().CreateDbContext());

        // Oturum yoksa (migration, arka plan işleri, seed) kullanılan boş bağlam.
        // AddQuamiIdentity çağrılırsa gerçek uygulama bunun yerini alır.
        services.TryAddScoped<ITenantContext, NullTenantContext>();

        services.TryAddSingleton(TimeProvider.System);
        services.Configure<FileStorageOptions>(configuration.GetSection(FileStorageOptions.SectionName));
        services.AddScoped<IFileStorage, LocalFileStorage>();

        services.AddScoped<IModuleLicenseService, ModuleLicenseService>();
        services.AddScoped<IMenuService, MenuService>();

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
