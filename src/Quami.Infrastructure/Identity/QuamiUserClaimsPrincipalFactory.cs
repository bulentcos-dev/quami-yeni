using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Quami.Application.Abstractions;
using Quami.Infrastructure.Persistence;

namespace Quami.Infrastructure.Identity;

/// <summary>
/// Oturum biletine kiracı ve sistem yöneticisi bilgisini ekler. Bu bilgi
/// Domain'deki User kaydından okunur ve her bilet üretiminde tazelenir.
/// </summary>
public sealed class QuamiUserClaimsPrincipalFactory(
    UserManager<QuamiIdentityUser> userManager,
    RoleManager<QuamiIdentityRole> roleManager,
    IOptions<IdentityOptions> options,
    QuamiDbContext db)
    : UserClaimsPrincipalFactory<QuamiIdentityUser, QuamiIdentityRole>(userManager, roleManager, options)
{
    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(QuamiIdentityUser user)
    {
        var identity = await base.GenerateClaimsAsync(user);

        // Kiracı süzgeci yok sayılır: bilet üretilirken kiracı bağlamı HENÜZ yok,
        // zaten burada kurulur. Silinmiş kayıt süzgeci açık kalır.
        var businessUser = await db.BusinessUsers
            .IgnoreQueryFilters([QuamiDbContext.TenantFilter])
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == user.Id);

        if (businessUser is null)
            return identity;

        identity.AddClaim(new Claim(QuamiClaimTypes.TenantId, businessUser.TenantId.ToString()));
        identity.AddClaim(new Claim(QuamiClaimTypes.IsSystemAdmin,
            businessUser.IsSystemAdmin ? "true" : "false"));

        return identity;
    }
}
