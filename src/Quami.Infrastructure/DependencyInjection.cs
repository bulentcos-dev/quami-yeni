using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Quami.Infrastructure.Persistence;

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

        return services;
    }
}
