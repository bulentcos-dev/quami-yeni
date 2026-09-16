using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Quami.Application.Abstractions;
using Quami.Domain.Enums;
using Quami.Infrastructure.Persistence;

namespace Quami.Infrastructure.Identity;

/// <summary>
/// Yerel kullanıcı adı/parola doğrulaması (ASP.NET Core Identity).
///
/// Eleme sırası: kurum → kullanıcı → parola. Her aşama kimlik olay günlüğüne
/// kendi sebebiyle yazılır, AMA ekrana dönen mesaj "kurum yok", "kullanıcı yok"
/// ve "parola yanlış" durumlarında aynıdır: kullanıcı adı taraması yapılamasın.
/// Parola hiçbir aşamada günlüğe geçmez.
/// </summary>
public sealed class LocalAuthenticationProvider(
    SignInManager<QuamiIdentityUser> signInManager,
    UserManager<QuamiIdentityUser> userManager,
    QuamiDbContext db,
    IAuthEventLog authEventLog,
    ITenantContext tenantContext)
    : IAuthenticationProvider
{
    public string Scheme => "Local";
    public bool SupportsPasswordSignIn => true;

    public async Task<SignInStatus> SignInWithPasswordAsync(
        string tenantShortName, string userName, string password, bool rememberMe,
        CancellationToken cancellationToken = default)
    {
        tenantShortName = tenantShortName.Trim();
        userName = userName.Trim();

        var tenant = await db.Tenants
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.ShortName.ToUpper() == tenantShortName.ToUpper(), cancellationToken);

        if (tenant is null)
            return await FailAsync(AuthFailureReason.TenantNotFound, tenantShortName, userName, null, null, cancellationToken);

        if (!tenant.IsActive)
            return await FailAsync(AuthFailureReason.TenantInactive, tenantShortName, userName, tenant.Id, null, cancellationToken);

        // Kurumun kayıtlı yazımı kullanılır: kullanıcı "demo" yazsa da eşleşir.
        var identityUserName = QuamiIdentityUser.ComposeUserName(tenant.ShortName, userName);

        var identityUser = await userManager.FindByNameAsync(identityUserName);
        if (identityUser is null)
            return await FailAsync(AuthFailureReason.UserNotFound, tenantShortName, userName, tenant.Id, null, cancellationToken);

        // Kiracı süzgeci yok sayılır: oturum henüz açılmadı, bağlam yok.
        var businessUser = await db.BusinessUsers
            .IgnoreQueryFilters([QuamiDbContext.TenantFilter])
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == identityUser.Id, cancellationToken);

        if (businessUser is null)
            return await FailAsync(AuthFailureReason.UserNotFound, tenantShortName, userName, tenant.Id, identityUser.Id, cancellationToken);

        if (!businessUser.IsActive)
            return await FailAsync(AuthFailureReason.UserInactive, tenantShortName, userName, tenant.Id, businessUser.Id, cancellationToken);

        // Kilidin bu denemede mi düştüğünü anlamak için önceki durum okunur.
        var wasLockedOut = await userManager.IsLockedOutAsync(identityUser);

        var result = await signInManager.PasswordSignInAsync(
            identityUser, password, rememberMe, lockoutOnFailure: true);

        if (result.Succeeded)
        {
            var tracked = await db.BusinessUsers
                .IgnoreQueryFilters([QuamiDbContext.TenantFilter])
                .FirstAsync(u => u.Id == identityUser.Id, cancellationToken);
            tracked.LastLoginAt = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellationToken);

            await authEventLog.RecordAsync(new AuthEventEntry
            {
                EventType = AuthEventType.SignInSucceeded,
                TenantShortName = tenant.ShortName,
                AttemptedUserName = userName,
                TenantId = tenant.Id,
                UserId = businessUser.Id
            }, cancellationToken);

            return SignInStatus.Success;
        }

        if (result.IsLockedOut)
        {
            if (!wasLockedOut)
            {
                // Kilit bu denemede düştü: ayrı bir olay olarak kaydedilir.
                await authEventLog.RecordAsync(new AuthEventEntry
                {
                    EventType = AuthEventType.AccountLockedOut,
                    TenantShortName = tenant.ShortName,
                    AttemptedUserName = userName,
                    TenantId = tenant.Id,
                    UserId = businessUser.Id
                }, cancellationToken);
            }

            return await FailAsync(AuthFailureReason.LockedOut, tenant.ShortName, userName, tenant.Id, businessUser.Id, cancellationToken);
        }

        if (result.RequiresTwoFactor)
            return await FailAsync(AuthFailureReason.RequiresTwoFactor, tenant.ShortName, userName, tenant.Id, businessUser.Id, cancellationToken);

        return await FailAsync(AuthFailureReason.InvalidPassword, tenant.ShortName, userName, tenant.Id, businessUser.Id, cancellationToken);
    }

    public async Task SignOutAsync(CancellationToken cancellationToken = default)
    {
        // Çıkan kullanıcı, çerez silinmeden önce okunur.
        var userId = tenantContext.UserId;
        var tenantId = tenantContext.TenantId;
        string? tenantShortName = null;
        string? userName = null;

        if (userId is { } id)
        {
            var user = await db.BusinessUsers
                .IgnoreQueryFilters([QuamiDbContext.TenantFilter])
                .AsNoTracking()
                .Include(u => u.Tenant)
                .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

            tenantShortName = user?.Tenant.ShortName;
            userName = user?.UserName;
        }

        await signInManager.SignOutAsync();

        await authEventLog.RecordAsync(new AuthEventEntry
        {
            EventType = AuthEventType.SignedOut,
            TenantShortName = tenantShortName,
            AttemptedUserName = userName,
            TenantId = tenantId,
            UserId = userId
        }, cancellationToken);
    }

    public async Task<bool> ChangePasswordAsync(
        Guid userId, string currentPassword, string newPassword,
        CancellationToken cancellationToken = default)
    {
        var businessUser = await db.BusinessUsers
            .IgnoreQueryFilters([QuamiDbContext.TenantFilter])
            .AsNoTracking()
            .Include(u => u.Tenant)
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        var identityUser = await userManager.FindByIdAsync(userId.ToString());

        var succeeded = false;
        if (identityUser is not null)
        {
            var result = await userManager.ChangePasswordAsync(identityUser, currentPassword, newPassword);
            succeeded = result.Succeeded;
        }

        await authEventLog.RecordAsync(new AuthEventEntry
        {
            EventType = AuthEventType.PasswordChanged,
            FailureReason = succeeded ? null : AuthFailureReason.PasswordChangeRejected,
            TenantShortName = businessUser?.Tenant.ShortName,
            AttemptedUserName = businessUser?.UserName,
            TenantId = businessUser?.TenantId,
            UserId = userId
        }, cancellationToken);

        return succeeded;
    }

    /// <summary>
    /// Başarısız denemeyi gerçek sebebiyle günlüğe yazar, ekrana ise ayrım
    /// yapmayan bir sonuç döndürür.
    /// </summary>
    private async Task<SignInStatus> FailAsync(
        AuthFailureReason reason, string? tenantShortName, string? userName,
        Guid? tenantId, Guid? userId, CancellationToken cancellationToken)
    {
        await authEventLog.RecordAsync(new AuthEventEntry
        {
            EventType = AuthEventType.SignInFailed,
            FailureReason = reason,
            TenantShortName = tenantShortName,
            AttemptedUserName = userName,
            TenantId = tenantId,
            UserId = userId
        }, cancellationToken);

        return reason switch
        {
            // Bu üçü ekranda AYNI mesajı verir; ayrım yalnızca günlüktedir.
            AuthFailureReason.TenantNotFound => SignInStatus.InvalidCredentials,
            AuthFailureReason.UserNotFound => SignInStatus.InvalidCredentials,
            AuthFailureReason.InvalidPassword => SignInStatus.InvalidCredentials,

            AuthFailureReason.LockedOut => SignInStatus.LockedOut,
            AuthFailureReason.RequiresTwoFactor => SignInStatus.RequiresTwoFactor,
            _ => SignInStatus.NotAllowed
        };
    }
}
