using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Quami.Application.Abstractions;
using Quami.Infrastructure.Persistence;
using Quami.Infrastructure.Services;

namespace Quami.Infrastructure;

public static class DependencyInjection
{
    public const string ConnectionStringName = "QuamiDb";

    /// <summary>
    /// Infrastructure servislerini kaydeder. Web ve Api projeleri Program.cs'te çağırır.
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

        // GEÇİCİ: oturum yok sayan boş bağlam. Adım 6'da oturumdan beslenen
        // gerçek uygulama kaydedilecek ve bunun yerini alacak. TryAdd olduğu
        // için o kayıt eklendiğinde burası devreye girmez.
        services.TryAddScoped<ITenantContext, NullTenantContext>();

        services.TryAddSingleton(TimeProvider.System);
        services.AddScoped<IModuleLicenseService, ModuleLicenseService>();

        return services;
    }
}
