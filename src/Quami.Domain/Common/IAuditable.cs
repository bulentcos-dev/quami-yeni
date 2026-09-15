namespace Quami.Domain.Common;

/// <summary>
/// Oluşturma ve güncelleme bilgisi taşıyan varlık.
/// SaveChanges override'ı (adım 3) bu alanları otomatik doldurur.
/// </summary>
public interface IAuditable
{
    DateTime CreatedAt { get; set; }
    Guid? CreatedByUserId { get; set; }
    DateTime? UpdatedAt { get; set; }
    Guid? UpdatedByUserId { get; set; }
}
