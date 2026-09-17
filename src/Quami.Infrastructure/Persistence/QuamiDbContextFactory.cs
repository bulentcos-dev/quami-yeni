using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Quami.Infrastructure.Persistence;

/// <summary>
/// `dotnet ef` komutları için bağlam fabrikası. Web host'unu ayağa kaldırmadan,
/// ITenantContext kaydı gerektirmeden çalışır.
///
/// Bağlantı dizesi <c>QUAMI_DB</c> ortam değişkeninden okunur. Verilmezse yerel
/// geliştirme varsayılanı kurulur: veritabanı kullanıcısı olarak makinedeki
/// oturum adı kullanılır (Homebrew ile kurulan PostgreSQL'in varsayılanı budur).
/// Böylece koda hiçbir makineye özel ad gömülmez.
/// </summary>
public sealed class QuamiDbContextFactory : IDesignTimeDbContextFactory<QuamiDbContext>
{
    public const string EnvironmentVariableName = "QUAMI_DB";

    /// <summary>Ortam değişkeni yoksa kullanılacak yerel geliştirme bağlantısı.</summary>
    public static string LocalDevelopmentConnectionString =>
        $"Host=localhost;Port=5432;Database=quami_dev;Username={Environment.UserName}";

    public QuamiDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable(EnvironmentVariableName)
            ?? LocalDevelopmentConnectionString;

        var options = new DbContextOptionsBuilder<QuamiDbContext>()
            .UseNpgsql(connectionString)
            .UseSnakeCaseNamingConvention()
            .Options;

        return new QuamiDbContext(options, new NullTenantContext());
    }
}
