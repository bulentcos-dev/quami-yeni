using Quami.Domain.Entities;

namespace Quami.Application.Abstractions;

/// <summary>
/// Modül lisans kontrolü. Kural: kiracı için lisansı olmayan modül menüde
/// görünmez ve doğrudan adres yazılarak da açılamaz.
/// Ürün olarak hazır olmayan modül (IsAvailable = false) hiçbir kiracı için
/// etkin sayılmaz, lisans kaydı olsa bile.
/// </summary>
public interface IModuleLicenseService
{
    /// <summary>Modül bu kiracı için şu an etkin mi?</summary>
    Task<bool> IsEnabledAsync(Guid tenantId, string moduleCode, CancellationToken cancellationToken = default);

    /// <summary>Kiracının etkin modülleri, menü sırasına göre (grup sırası, sonra modül sırası).</summary>
    Task<IReadOnlyList<Module>> GetEnabledModulesAsync(Guid tenantId, CancellationToken cancellationToken = default);
}
