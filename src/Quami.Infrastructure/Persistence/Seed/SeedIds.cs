namespace Quami.Infrastructure.Persistence.Seed;

/// <summary>
/// Sistem geneli kayıtların SABİT kimlikleri. Elle yazılır, çalıştırmada
/// üretilmez: aynı modül lokalde, test sunucusunda ve canlıda aynı kimliği
/// taşır. Seed eşleştirmesi bu kimlik üzerinden yapılır, ad üzerinden DEĞİL —
/// ad değişebilir, kimlik değişmez.
///
/// KURAL: buradaki bir değer bir kez canlıya çıktıktan sonra ASLA değişmez.
/// Yeni modül eklerken yeni bir numara ver, var olanı yeniden kullanma.
/// </summary>
public static class SeedIds
{
    // Menü grupları: 1000....-00NN
    public static class MenuGroups
    {
        public static readonly Guid Home   = new("10000000-0000-0000-0000-000000000001");
        public static readonly Guid MyWork = new("10000000-0000-0000-0000-000000000002");
        public static readonly Guid DocRec = new("10000000-0000-0000-0000-000000000003");
        public static readonly Guid Audit  = new("10000000-0000-0000-0000-000000000004");
        public static readonly Guid Risk   = new("10000000-0000-0000-0000-000000000005");
        public static readonly Guid System = new("10000000-0000-0000-0000-000000000006");
    }

    // Modüller: 2000....-00NN
    public static class Modules
    {
        public static readonly Guid Home       = new("20000000-0000-0000-0000-000000000001");
        public static readonly Guid Tasks      = new("20000000-0000-0000-0000-000000000002");
        public static readonly Guid Suggest    = new("20000000-0000-0000-0000-000000000003");
        public static readonly Guid Capa       = new("20000000-0000-0000-0000-000000000004");
        public static readonly Guid Docs       = new("20000000-0000-0000-0000-000000000005");
        public static readonly Guid Contracts  = new("20000000-0000-0000-0000-000000000006");
        public static readonly Guid Records    = new("20000000-0000-0000-0000-000000000007");
        public static readonly Guid AuditMgmt  = new("20000000-0000-0000-0000-000000000008");
        public static readonly Guid Compliance = new("20000000-0000-0000-0000-000000000009");
        public static readonly Guid Perf       = new("20000000-0000-0000-0000-00000000000a");
        public static readonly Guid Supplier   = new("20000000-0000-0000-0000-00000000000b");
        public static readonly Guid Training   = new("20000000-0000-0000-0000-00000000000c");
        public static readonly Guid Survey     = new("20000000-0000-0000-0000-00000000000d");
        public static readonly Guid Maint      = new("20000000-0000-0000-0000-00000000000e");
        public static readonly Guid Threat     = new("20000000-0000-0000-0000-00000000000f");
        public static readonly Guid InfoSec    = new("20000000-0000-0000-0000-000000000010");
        public static readonly Guid Quality    = new("20000000-0000-0000-0000-000000000011");
        public static readonly Guid Ohs        = new("20000000-0000-0000-0000-000000000012");
        public static readonly Guid Env        = new("20000000-0000-0000-0000-000000000013");
        public static readonly Guid Food       = new("20000000-0000-0000-0000-000000000014");
        public static readonly Guid Reports    = new("20000000-0000-0000-0000-000000000015");
        public static readonly Guid Users      = new("20000000-0000-0000-0000-000000000016");
        public static readonly Guid Defs       = new("20000000-0000-0000-0000-000000000017");
    }

    /// <summary>Yalnızca boş veritabanında oluşturulan örnek kiracı ve kullanıcısı.</summary>
    public static class Sample
    {
        public static readonly Guid Tenant = new("30000000-0000-0000-0000-000000000001");
        public static readonly Guid User   = new("40000000-0000-0000-0000-000000000001");
    }
}
