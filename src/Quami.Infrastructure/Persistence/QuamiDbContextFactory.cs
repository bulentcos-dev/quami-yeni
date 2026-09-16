using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Quami.Infrastructure.Persistence;

/// <summary>
/// `dotnet ef` komutları için bağlam fabrikası. Web host'unu ayağa kaldırmadan,
/// ITenantContext kaydı gerektirmeden çalışır.
/// Bağlantı dizesi: QUAMI_DB ortam değişkeni; yoksa yerel geliştirme varsayılanı.
/// </summary>
public sealed class QuamiDbContextFactory : IDesignTimeDbContextFactory<QuamiDbContext>
{
    public const string DefaultLocalConnectionString =
        "Host=localhost;Port=5432;Database=quami_dev;Username=<kullanici>";

    public QuamiDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("QUAMI_DB")
            ?? DefaultLocalConnectionString;

        var options = new DbContextOptionsBuilder<QuamiDbContext>()
            .UseNpgsql(connectionString)
            .UseSnakeCaseNamingConvention()
            .Options;

        return new QuamiDbContext(options, new NullTenantContext());
    }
}
