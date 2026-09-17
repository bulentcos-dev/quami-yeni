using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Quami.Domain.Common;
using Quami.Domain.Entities;
using Quami.Domain.Enums;
using Quami.Infrastructure.Identity;

namespace Quami.Infrastructure.Persistence.Seed;

/// <summary>
/// Açılışta çalışan seed.
///
/// Sistem geneli kayıtlar (MenuGroups, Modules): her açılışta koda göre
/// eklenir veya GÜNCELLENİR. Kodda modülün adını, sırasını, ikonunu, adresini
/// veya hazır olma durumunu değiştirmek yeter; bir sonraki açılışta yansır.
/// Eşleştirme sabit kimlik üzerindendir (<see cref="SeedIds"/>), ad üzerinden değil.
///
/// Kiracıya ait veriler (Tenants, Users, TenantModules): yalnızca hiç kiracı
/// yokken, yani boş veritabanında oluşturulur. Var olan kiracı verisine ASLA
/// dokunulmaz — müşteri bir modülün lisansını elle kapattıysa açılışta geri
/// açılmaz, tarih aralığı değiştirilmez.
/// </summary>
public static class DatabaseSeeder
{
    /// <summary>
    /// Seed kilidinin anahtarı. ASCII "QuamiSed". Aynı veritabanına aynı anda
    /// açılan iki uygulama örneğinden yalnızca biri seed'i çalıştırsın diye.
    /// DEĞİŞTİRMEYİN: değer değişirse eski sürümle yeni sürüm birbirini beklemez.
    /// </summary>
    public const long SeedAdvisoryLockKey = 0x5175_616D_6953_6564;

    /// <summary>
    /// Migration'ları (isteğe bağlı) uygular, sonra seed'i çalıştırır.
    /// </summary>
    /// <param name="applyMigrations">
    /// Geliştirmede true. Test/canlıda migration'ı dağıtım adımı uygular,
    /// uygulama açılışı değil.
    /// </param>
    /// <param name="sampleUserPassword">
    /// Doluysa örnek kullanıcıya bu parolayla bir kimlik kaydı açılır.
    /// YALNIZCA GELİŞTİRMEDE verilir; test ve canlıda null geçilir, orada
    /// kullanıcılar elle veya kurulum adımıyla açılır.
    /// </param>
    public static async Task MigrateAndSeedAsync(
        IServiceProvider services, bool applyMigrations, string? sampleUserPassword = null,
        CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<QuamiDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>()
            .CreateLogger(typeof(DatabaseSeeder).FullName!);

        if (applyMigrations)
        {
            // Migration'ın kendi kilidi EF Core tarafından yönetilir.
            logger.LogInformation("Migration'lar uygulanıyor.");
            await db.Database.MigrateAsync(cancellationToken);
        }

        // Seed tek işlemde ve tek örnekte çalışır. İkinci örnek kilidi bekler;
        // kilit düşünce işin yapılmış olduğunu görür ve değişiklik yazmadan geçer.
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        if (db.Database.IsNpgsql())
        {
            logger.LogInformation("Seed kilidi bekleniyor.");
            // pg_advisory_xact_lock: işlem bitince (commit/rollback) kendiliğinden
            // bırakılır. Uygulama çökse bile kilit sızmaz.
            await db.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT pg_advisory_xact_lock({SeedAdvisoryLockKey})", cancellationToken);
        }

        var changed = await SeedSystemDataAsync(db, cancellationToken);
        logger.LogInformation("Sistem geneli seed tamam ({Changed} kayıt eklendi/güncellendi).", changed);

        if (await SeedSampleTenantAsync(db, cancellationToken))
            logger.LogInformation("Boş veritabanı: örnek kiracı oluşturuldu.");

        if (!string.IsNullOrEmpty(sampleUserPassword))
        {
            await EnsureSampleIdentityUserAsync(scope.ServiceProvider, db, sampleUserPassword, logger, cancellationToken);
        }
        else
        {
            logger.LogInformation(
                "Örnek kullanıcı için parola verilmedi, giriş kaydı açılmadı. " +
                "Geliştirmede açmak için Seed:SampleUserPassword ayarını verin " +
                "(bkz. README, Ayarlar bölümü).");
        }

        await transaction.CommitAsync(cancellationToken);
    }

    /// <summary>Menü grupları ve modüller: yoksa ekle, varsa tanım alanlarını güncelle.</summary>
    private static async Task<int> SeedSystemDataAsync(QuamiDbContext db, CancellationToken cancellationToken)
    {
        // Silinmiş kayıtlar da okunur: kimlik birincil anahtarı işgal eder,
        // yeniden eklemek çakışır. Silinmişse canlandırılır.
        var groups = await db.MenuGroups.IgnoreQueryFilters()
            .ToDictionaryAsync(g => g.Id, cancellationToken);

        foreach (var def in SeedData.MenuGroups)
        {
            if (!groups.TryGetValue(def.Id, out var group))
            {
                db.MenuGroups.Add(new MenuGroup
                {
                    Id = def.Id,
                    Code = def.Code,
                    NameTr = def.NameTr,
                    NameEn = def.NameEn,
                    SortOrder = def.SortOrder,
                    Icon = def.Icon,
                    IsDirectLink = def.IsDirectLink
                });
                continue;
            }

            group.Code = def.Code;
            group.NameTr = def.NameTr;
            group.NameEn = def.NameEn;
            group.SortOrder = def.SortOrder;
            group.Icon = def.Icon;
            group.IsDirectLink = def.IsDirectLink;
            Revive(group);
        }

        var modules = await db.Modules.IgnoreQueryFilters()
            .ToDictionaryAsync(m => m.Id, cancellationToken);

        foreach (var def in SeedData.Modules)
        {
            if (!modules.TryGetValue(def.Id, out var module))
            {
                db.Modules.Add(new Module
                {
                    Id = def.Id,
                    Code = def.Code,
                    NameTr = def.NameTr,
                    NameEn = def.NameEn,
                    MenuGroupId = def.MenuGroupId,
                    SortOrder = def.SortOrder,
                    Icon = def.Icon,
                    Route = def.Route,
                    IsAvailable = def.IsAvailable
                });
                continue;
            }

            module.Code = def.Code;
            module.NameTr = def.NameTr;
            module.NameEn = def.NameEn;
            module.MenuGroupId = def.MenuGroupId;
            module.SortOrder = def.SortOrder;
            module.Icon = def.Icon;
            module.Route = def.Route;
            module.IsAvailable = def.IsAvailable;
            Revive(module);
        }

        return await db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Örnek kiracı: YALNIZCA hiç kiracı yokken. Hazır modüllerin hepsi açılır;
    /// hazır olmayan (ENV, FOOD) için lisans kaydı oluşturulmaz.
    /// </summary>
    private static async Task<bool> SeedSampleTenantAsync(QuamiDbContext db, CancellationToken cancellationToken)
    {
        if (await db.Tenants.IgnoreQueryFilters().AnyAsync(cancellationToken))
            return false;

        db.Tenants.Add(new Tenant
        {
            Id = SeedIds.Sample.Tenant,
            Name = "Örnek Kurum",
            ShortName = "DEMO",
            IsActive = true
        });

        db.BusinessUsers.Add(new User
        {
            Id = SeedIds.Sample.User,
            TenantId = SeedIds.Sample.Tenant,
            UserName = "admin",
            Email = "admin@demo.local",
            FullName = "Örnek Yönetici",
            IsActive = true,
            IsSystemAdmin = false,
            Language = Language.Tr
        });

        foreach (var def in SeedData.Modules.Where(m => m.IsAvailable))
        {
            db.TenantModules.Add(new TenantModule
            {
                TenantId = SeedIds.Sample.Tenant,
                ModuleId = def.Id,
                IsActive = true,
                StartDate = null,
                EndDate = null
            });
        }

        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    /// <summary>
    /// Örnek kullanıcının kimlik kaydını (parola) açar. Yalnızca geliştirmede
    /// çağrılır. Kayıt zaten varsa dokunulmaz — parola sıfırlanmaz.
    /// </summary>
    private static async Task EnsureSampleIdentityUserAsync(
        IServiceProvider scopedServices, QuamiDbContext db, string password,
        ILogger logger, CancellationToken cancellationToken)
    {
        var userManager = scopedServices.GetService<UserManager<QuamiIdentityUser>>();
        if (userManager is null)
            return; // Kimlik kayıtlı değil (ör. Api projesi): yapacak bir şey yok.

        var businessUser = await db.BusinessUsers
            .IgnoreQueryFilters([QuamiDbContext.TenantFilter])
            .Include(u => u.Tenant)
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == SeedIds.Sample.User, cancellationToken);

        if (businessUser is null)
            return;

        var identityUserName = QuamiIdentityUser.ComposeUserName(
            businessUser.Tenant.ShortName, businessUser.UserName);

        if (await userManager.FindByNameAsync(identityUserName) is not null)
            return;

        var identityUser = new QuamiIdentityUser
        {
            Id = businessUser.Id, // İş kaydıyla aynı kimlik
            UserName = identityUserName,
            Email = businessUser.Email,
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(identityUser, password);
        if (!result.Succeeded)
        {
            logger.LogWarning("Örnek kullanıcının kimlik kaydı açılamadı: {Errors}",
                string.Join("; ", result.Errors.Select(e => e.Description)));
            return;
        }

        logger.LogWarning(
            "GELİŞTİRME: örnek kullanıcı '{UserName}' bilinen bir parolayla oluşturuldu. " +
            "Bu hesap test ve canlı ortamda BULUNMAMALIDIR.", identityUserName);
    }

    /// <summary>Kodda duran bir kayıt silinmiş işaretliyse geri getirir.</summary>
    private static void Revive(BaseEntity entity)
    {
        if (!entity.IsDeleted) return;
        entity.IsDeleted = false;
        entity.DeletedAt = null;
        entity.DeletedByUserId = null;
    }
}
