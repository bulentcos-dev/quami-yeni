using System.Linq.Expressions;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Quami.Application.Abstractions;
using Quami.Domain.Common;
using Quami.Domain.Entities;
using Quami.Domain.Enums;

namespace Quami.Infrastructure.Persistence;

/// <summary>
/// Quami veritabanı bağlamı.
/// - Global query filter: ITenantScoped varlıklarda kiracı süzgeci (sistem
///   yöneticisi muaf), ISoftDeletable varlıklarda silinmiş kayıt gizleme.
/// - SaveChanges override: TenantId ve denetim alanlarını doldurur, fiziksel
///   silmeyi mantıksal silmeye çevirir, AuditLog yazar.
/// </summary>
public class QuamiDbContext : DbContext
{
    public const string TenantFilter = "Tenant";
    public const string SoftDeleteFilter = "SoftDelete";

    private readonly ITenantContext _tenantContext;

    public QuamiDbContext(DbContextOptions<QuamiDbContext> options, ITenantContext tenantContext)
        : base(options)
    {
        _tenantContext = tenantContext;
    }

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<MenuGroup> MenuGroups => Set<MenuGroup>();
    public DbSet<Module> Modules => Set<Module>();
    public DbSet<TenantModule> TenantModules => Set<TenantModule>();
    public DbSet<User> Users => Set<User>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    // Query filter'lar bu iki özelliği yakalar; EF Core bunları sorgu parametresi
    // olarak gönderir, plan önbelleği bozulmaz.
    private Guid? CurrentTenantId => _tenantContext.TenantId;
    private bool BypassTenantFilter => _tenantContext.IsSystemAdmin;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(QuamiDbContext).Assembly);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var clrType = entityType.ClrType;

            if (typeof(ISoftDeletable).IsAssignableFrom(clrType))
            {
                var method = typeof(QuamiDbContext)
                    .GetMethod(nameof(ApplySoftDeleteFilter), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
                    .MakeGenericMethod(clrType);
                method.Invoke(this, [modelBuilder]);
            }

            if (typeof(ITenantScoped).IsAssignableFrom(clrType))
            {
                var method = typeof(QuamiDbContext)
                    .GetMethod(nameof(ApplyTenantFilter), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
                    .MakeGenericMethod(clrType);
                method.Invoke(this, [modelBuilder]);
            }
        }
    }

    private void ApplySoftDeleteFilter<TEntity>(ModelBuilder modelBuilder)
        where TEntity : class, ISoftDeletable
    {
        modelBuilder.Entity<TEntity>().HasQueryFilter(SoftDeleteFilter, e => !e.IsDeleted);
    }

    private void ApplyTenantFilter<TEntity>(ModelBuilder modelBuilder)
        where TEntity : class, ITenantScoped
    {
        // Sistem yöneticisi: süzgeç yok. Diğerleri: yalnızca kendi kiracısı.
        // Kiracı yoksa (oturum dışı) hiçbir satır dönmez; güvenli varsayılan.
        Expression<Func<TEntity, bool>> filter =
            e => BypassTenantFilter || (CurrentTenantId != null && e.TenantId == CurrentTenantId);
        modelBuilder.Entity<TEntity>().HasQueryFilter(TenantFilter, filter);
    }

    // Denetim günlüğü ana kayıtla AYNI SaveChanges içinde yazılır; EF Core tek
    // SaveChanges'i tek işlemde (transaction) çalıştırır. Ya ikisi birlikte
    // yazılır ya birlikte geri alınır. Insert'te Id istemci tarafında (Guid v7)
    // üretildiği için RecordId kaydetmeden önce bilinir.
    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        var auditEntries = OnBeforeSaveChanges();
        if (auditEntries.Count > 0)
            AuditLogs.AddRange(auditEntries);
        return base.SaveChanges(acceptAllChangesOnSuccess) - auditEntries.Count;
    }

    public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        var auditEntries = OnBeforeSaveChanges();
        if (auditEntries.Count > 0)
            AuditLogs.AddRange(auditEntries);
        return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken) - auditEntries.Count;
    }

    /// <summary>
    /// Kaydetmeden önce: TenantId ve denetim alanlarını doldurur, silmeyi
    /// mantıksala çevirir, yazılacak AuditLog kayıtlarını hazırlar.
    /// </summary>
    private List<AuditLog> OnBeforeSaveChanges()
    {
        ChangeTracker.DetectChanges();

        var now = DateTime.UtcNow;
        var userId = _tenantContext.UserId;
        var auditEntries = new List<AuditLog>();

        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.Entity is AuditLog)
                continue;

            if (entry.State is EntityState.Detached or EntityState.Unchanged)
                continue;

            switch (entry.State)
            {
                case EntityState.Added:
                    if (entry.Entity is ITenantScoped scoped && scoped.TenantId == Guid.Empty)
                    {
                        scoped.TenantId = _tenantContext.TenantId
                            ?? throw new InvalidOperationException(
                                $"{entry.Metadata.ClrType.Name} kaydı için kiracı bağlamı yok (TenantId boş).");
                    }
                    if (entry.Entity is IAuditable addedAuditable)
                    {
                        addedAuditable.CreatedAt = now;
                        addedAuditable.CreatedByUserId = userId;
                    }
                    break;

                case EntityState.Modified:
                    if (entry.Entity is IAuditable modifiedAuditable)
                    {
                        modifiedAuditable.UpdatedAt = now;
                        modifiedAuditable.UpdatedByUserId = userId;
                        // Oluşturma bilgisi güncellemede değişmez.
                        entry.Property(nameof(IAuditable.CreatedAt)).IsModified = false;
                        entry.Property(nameof(IAuditable.CreatedByUserId)).IsModified = false;
                    }
                    break;

                case EntityState.Deleted:
                    if (entry.Entity is ISoftDeletable softDeletable)
                    {
                        // Fiziksel silme yok: işaretle ve güncellemeye çevir.
                        // State=Modified tüm sütunları "değişti" sayar; yalnızca
                        // silme ve güncelleme alanları gönderilsin.
                        entry.State = EntityState.Modified;
                        foreach (var p in entry.Properties)
                            p.IsModified = false;

                        softDeletable.IsDeleted = true;
                        softDeletable.DeletedAt = now;
                        softDeletable.DeletedByUserId = userId;
                        entry.Property(nameof(ISoftDeletable.IsDeleted)).IsModified = true;
                        entry.Property(nameof(ISoftDeletable.DeletedAt)).IsModified = true;
                        entry.Property(nameof(ISoftDeletable.DeletedByUserId)).IsModified = true;

                        if (entry.Entity is IAuditable delAuditable)
                        {
                            delAuditable.UpdatedAt = now;
                            delAuditable.UpdatedByUserId = userId;
                            entry.Property(nameof(IAuditable.UpdatedAt)).IsModified = true;
                            entry.Property(nameof(IAuditable.UpdatedByUserId)).IsModified = true;
                        }
                        auditEntries.Add(BuildAuditLog(entry, AuditOperation.Delete, now, userId));
                        continue;
                    }
                    break;
            }

            var operation = entry.State switch
            {
                EntityState.Added => AuditOperation.Insert,
                EntityState.Modified => AuditOperation.Update,
                EntityState.Deleted => AuditOperation.Delete,
                _ => (AuditOperation?)null
            };
            if (operation is not null)
                auditEntries.Add(BuildAuditLog(entry, operation.Value, now, userId));
        }

        return auditEntries;
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false,
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
    };

    private static AuditLog BuildAuditLog(EntityEntry entry, AuditOperation operation, DateTime now, Guid? userId)
    {
        Dictionary<string, object?>? oldValues = operation == AuditOperation.Insert ? null : new();
        Dictionary<string, object?>? newValues = operation == AuditOperation.Delete && entry.State == EntityState.Deleted ? null : new();

        foreach (var property in entry.Properties)
        {
            var name = property.Metadata.Name;

            switch (operation)
            {
                case AuditOperation.Insert:
                    newValues![name] = property.CurrentValue;
                    break;

                case AuditOperation.Update:
                    if (property.IsModified)
                    {
                        oldValues![name] = property.OriginalValue;
                        newValues![name] = property.CurrentValue;
                    }
                    break;

                case AuditOperation.Delete:
                    oldValues![name] = property.OriginalValue;
                    if (newValues is not null && property.IsModified)
                        newValues[name] = property.CurrentValue;
                    break;
            }
        }

        var keyValues = entry.Properties
            .Where(p => p.Metadata.IsPrimaryKey())
            .Select(p => p.CurrentValue?.ToString() ?? string.Empty);

        return new AuditLog
        {
            TenantId = entry.Entity is ITenantScoped scoped ? scoped.TenantId : null,
            UserId = userId,
            TableName = entry.Metadata.GetTableName() ?? entry.Metadata.ClrType.Name,
            RecordId = string.Join("|", keyValues),
            Operation = operation,
            OldValues = oldValues is null ? null : JsonSerializer.Serialize(oldValues, JsonOptions),
            NewValues = newValues is null ? null : JsonSerializer.Serialize(newValues, JsonOptions),
            Timestamp = now
        };
    }
}
