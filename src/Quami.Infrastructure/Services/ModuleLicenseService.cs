using Microsoft.EntityFrameworkCore;
using Quami.Application.Abstractions;
using Quami.Domain.Entities;
using Quami.Infrastructure.Persistence;

namespace Quami.Infrastructure.Services;

/// <summary>
/// Lisans kontrolü. Bir modül ancak şu üç koşul birlikte sağlanırsa etkindir:
/// ürün olarak hazır (IsAvailable), kiracının lisans kaydı aktif, ve tarih
/// aralığı içinde. Hazır olmayan modül, lisans kaydı olsa bile etkin sayılmaz.
///
/// Sorgular kiracı süzgecini yok sayar ve kiracıyı açıkça parametreden alır:
/// sistem yöneticisi başka bir kiracının lisansını da sorgulayabilmeli.
/// Silinmiş kayıt süzgeci açık kalır.
/// </summary>
public sealed class ModuleLicenseService(IDbContextFactory<QuamiDbContext> dbFactory, TimeProvider timeProvider) : IModuleLicenseService
{
    public async Task<bool> IsEnabledAsync(Guid tenantId, string moduleCode, CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;

        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        return await db.Modules
            .IgnoreQueryFilters([QuamiDbContext.TenantFilter])
            .AnyAsync(m => m.Code == moduleCode && m.IsAvailable
                && m.TenantModules.Any(tm => tm.TenantId == tenantId && tm.IsActive
                    && (tm.StartDate == null || tm.StartDate <= now)
                    && (tm.EndDate == null || tm.EndDate > now)),
                cancellationToken);
    }

    public async Task<IReadOnlyList<Module>> GetEnabledModulesAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;

        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        return await db.Modules
            .IgnoreQueryFilters([QuamiDbContext.TenantFilter])
            .Include(m => m.MenuGroup)
            .Where(m => m.IsAvailable
                && m.TenantModules.Any(tm => tm.TenantId == tenantId && tm.IsActive
                    && (tm.StartDate == null || tm.StartDate <= now)
                    && (tm.EndDate == null || tm.EndDate > now)))
            .OrderBy(m => m.MenuGroup.SortOrder)
            .ThenBy(m => m.SortOrder)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }
}
