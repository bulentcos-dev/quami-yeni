using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Quami.Application.Menu;
using Quami.Domain.Entities;
using Quami.Infrastructure.Persistence;

namespace Quami.Infrastructure.Services;

/// <summary>
/// Menüyü MenuGroups/Modules tablolarından ve kiracının lisanslarından kurar.
///
/// Bir modül menüde şu iki durumda görünür:
/// 1. Hazır (IsAvailable) ve kiracının geçerli lisansı var → tıklanabilir.
/// 2. Hazır değil (ENV, FOOD) → soluk, "yakında", tıklanmaz.
/// Hazır olup lisansı olmayan modül menüde HİÇ görünmez.
///
/// Metin dili geçerli arayüz kültüründen seçilir (tr → NameTr, diğer → NameEn).
/// </summary>
public sealed class MenuService(IDbContextFactory<QuamiDbContext> dbFactory, TimeProvider timeProvider) : IMenuService
{
    public async Task<IReadOnlyList<MenuGroupView>> GetMenuAsync(
        Guid tenantId, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        var groups = await db.MenuGroups
            .AsNoTracking()
            .Include(g => g.Modules)
            .OrderBy(g => g.SortOrder)
            .ToListAsync(cancellationToken);

        var licensedModuleIds = await GetLicensedModuleIdsAsync(db, tenantId, cancellationToken);

        var result = new List<MenuGroupView>();

        foreach (var group in groups)
        {
            var items = group.Modules
                .Where(m => !m.IsAvailable || licensedModuleIds.Contains(m.Id))
                .OrderBy(m => m.SortOrder)
                .Select(m => new MenuItemView(m.Code, PickName(m), m.Route, m.Icon, m.IsAvailable))
                .ToList();

            if (items.Count == 0)
                continue;

            result.Add(new MenuGroupView(
                group.Code,
                PickName(group),
                group.Icon,
                group.IsDirectLink,
                group.IsDirectLink ? items[0].Route : null,
                items));
        }

        return result;
    }

    public async Task<ModuleAccess> CheckRouteAsync(
        Guid tenantId, string path, CancellationToken cancellationToken = default)
    {
        var normalized = Normalize(path);

        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        var module = await db.Modules
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Route.ToLower() == normalized, cancellationToken);

        if (module is null)
            return ModuleAccess.NotAModule;

        if (!module.IsAvailable)
            return ModuleAccess.NotAvailable;

        var licensedModuleIds = await GetLicensedModuleIdsAsync(db, tenantId, cancellationToken);
        return licensedModuleIds.Contains(module.Id) ? ModuleAccess.Allowed : ModuleAccess.NotLicensed;
    }

    private async Task<HashSet<Guid>> GetLicensedModuleIdsAsync(
        QuamiDbContext db, Guid tenantId, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;

        // Kiracı süzgeci yok sayılır, kiracı açıkça parametreden gelir:
        // sistem yöneticisi başka kiracının menüsünü de görebilmeli.
        var ids = await db.TenantModules
            .IgnoreQueryFilters([QuamiDbContext.TenantFilter])
            .Where(tm => tm.TenantId == tenantId && tm.IsActive
                && (tm.StartDate == null || tm.StartDate <= now)
                && (tm.EndDate == null || tm.EndDate > now))
            .Select(tm => tm.ModuleId)
            .ToListAsync(cancellationToken);

        return [.. ids];
    }

    /// <summary>Adresi karşılaştırılabilir hale getirir: küçük harf, sondaki eğik çizgi atılır.</summary>
    private static string Normalize(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return "/";
        path = path.Trim();
        if (!path.StartsWith('/')) path = "/" + path;
        var query = path.IndexOfAny(['?', '#']);
        if (query >= 0) path = path[..query];
        if (path.Length > 1) path = path.TrimEnd('/');
        return path.ToLowerInvariant();
    }

    private static string PickName(Module module) => IsTurkish ? module.NameTr : module.NameEn;
    private static string PickName(MenuGroup group) => IsTurkish ? group.NameTr : group.NameEn;

    private static bool IsTurkish =>
        CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.Equals("tr", StringComparison.OrdinalIgnoreCase);
}
