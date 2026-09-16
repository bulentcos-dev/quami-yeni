using Microsoft.AspNetCore.Identity;

namespace Quami.Infrastructure.Identity;

/// <summary>Rol kaydı. Yetkilendirme ayrıntısı sonraki adımlarda.</summary>
public class QuamiIdentityRole : IdentityRole<Guid>;
