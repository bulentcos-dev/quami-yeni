namespace Quami.Domain.Common;

/// <summary>
/// Mantıksal silme. Kayıt fiziksel silinmez, IsDeleted işaretlenir;
/// global query filter silinmiş kayıtları otomatik gizler.
/// ISO denetimlerinde kayıt izi kaybolmamalı.
/// </summary>
public interface ISoftDeletable
{
    bool IsDeleted { get; set; }
    DateTime? DeletedAt { get; set; }
    Guid? DeletedByUserId { get; set; }
}
