namespace Quami.Application.Abstractions;

/// <summary>Oturum biletine yazılan Quami'ye özel talep (claim) adları.</summary>
public static class QuamiClaimTypes
{
    public const string TenantId = "quami:tenant_id";
    public const string IsSystemAdmin = "quami:is_system_admin";
}
