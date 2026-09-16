namespace Quami.Infrastructure.Persistence.Seed;

/// <summary>Bir menü grubunun kod içindeki tanımı.</summary>
public sealed record MenuGroupSeed(
    Guid Id, string Code, string NameTr, string NameEn, int SortOrder, string Icon,
    bool IsDirectLink = false);

/// <summary>
/// Bir modülün kod içindeki tanımı. <paramref name="IsAvailable"/> false ise
/// modül "yakında" olarak görünür ve hiçbir kiracıya lisanslanmaz.
/// </summary>
public sealed record ModuleSeed(
    Guid Id, string Code, string NameTr, string NameEn, Guid MenuGroupId,
    int SortOrder, string Icon, string Route, bool IsAvailable = true);

/// <summary>
/// Menünün kaynağı. Burada yapılan değişiklik (ad, sıra, ikon, adres, hazır mı)
/// bir sonraki açılışta veritabanına yansır; eşleştirme kimlik üzerindendir.
/// </summary>
public static class SeedData
{
    public static readonly IReadOnlyList<MenuGroupSeed> MenuGroups =
    [
        new(SeedIds.MenuGroups.Home,   "HOME",   "Ana sayfa",                 "Home",               1, "house", IsDirectLink: true),
        new(SeedIds.MenuGroups.MyWork, "MYWORK", "İşlerim",                   "My Work",            2, "list-checks"),
        new(SeedIds.MenuGroups.DocRec, "DOCREC", "Süreç ve kayıt",            "Documents & Records", 3, "folder"),
        new(SeedIds.MenuGroups.Audit,  "AUDIT",  "Denetim ve değerlendirme",  "Audit & Assessment", 4, "clipboard-check"),
        new(SeedIds.MenuGroups.Risk,   "RISK",   "Risk",                      "Risk",               5, "shield-alert"),
        new(SeedIds.MenuGroups.System, "SYSTEM", "Sistem",                    "System",             6, "settings"),
    ];

    public static readonly IReadOnlyList<ModuleSeed> Modules =
    [
        // Ana sayfa
        new(SeedIds.Modules.Home,       "HOME",       "Ana sayfa",                  "Home",                     SeedIds.MenuGroups.Home,   1, "house",              "/"),

        // İşlerim — olay/aksiyon zinciri kalıbı
        new(SeedIds.Modules.Tasks,      "TASKS",      "Görevler",                   "Tasks",                    SeedIds.MenuGroups.MyWork, 1, "square-check-big",      "/tasks"),
        new(SeedIds.Modules.Suggest,    "SUGGEST",    "Öneriler",                   "Suggestions",              SeedIds.MenuGroups.MyWork, 2, "lightbulb",          "/suggestions"),
        new(SeedIds.Modules.Capa,       "CAPA",       "Düzeltici faaliyet",         "Corrective Action",        SeedIds.MenuGroups.MyWork, 3, "wrench",  "/corrective-actions"),

        // Süreç ve kayıt — kayıt + onay akışı kalıbı
        new(SeedIds.Modules.Docs,       "DOCS",       "Dokümanlar",                 "Documents",                SeedIds.MenuGroups.DocRec, 1, "file-text",  "/documents"),
        new(SeedIds.Modules.Contracts,  "CONTRACTS",  "Sözleşmeler",                "Contracts",                SeedIds.MenuGroups.DocRec, 2, "file-signature", "/contracts"),
        new(SeedIds.Modules.Records,    "RECORDS",    "Kayıtlar",                   "Records",                  SeedIds.MenuGroups.DocRec, 3, "archive",            "/records"),

        // Denetim ve değerlendirme — program döngüsü kalıbı
        new(SeedIds.Modules.AuditMgmt,  "AUDITMGMT",  "Denetim yönetimi",           "Audit Management",         SeedIds.MenuGroups.Audit,  1, "clipboard-check",    "/audit-management"),
        new(SeedIds.Modules.Compliance, "COMPLIANCE", "Uyum",                       "Compliance",               SeedIds.MenuGroups.Audit,  2, "badge-check",        "/compliance"),
        new(SeedIds.Modules.Perf,       "PERF",       "Performans yönetimi",        "Performance Management",   SeedIds.MenuGroups.Audit,  3, "trending-up",           "/performance"),
        new(SeedIds.Modules.Supplier,   "SUPPLIER",   "Tedarikçi değerleme",        "Supplier Evaluation",      SeedIds.MenuGroups.Audit,  4, "truck",              "/supplier-evaluation"),
        new(SeedIds.Modules.Training,   "TRAINING",   "Eğitim yönetimi",            "Training Management",      SeedIds.MenuGroups.Audit,  5, "graduation-cap",        "/training"),
        new(SeedIds.Modules.Survey,     "SURVEY",     "Anket yönetimi",             "Survey Management",        SeedIds.MenuGroups.Audit,  6, "list-todo",          "/survey"),
        new(SeedIds.Modules.Maint,      "MAINT",      "Arıza bakım-kalibrasyon",    "Maintenance & Calibration", SeedIds.MenuGroups.Audit, 7, "gauge",              "/maintenance"),

        // Risk — envanter + analiz + tedbir kalıbı
        new(SeedIds.Modules.Threat,     "THREAT",     "Tehdit modeli",              "Threat Model",             SeedIds.MenuGroups.Risk,   1, "network",          "/threat-model"),
        new(SeedIds.Modules.InfoSec,    "INFOSEC",    "Bilgi güvenliği",            "Information Security",     SeedIds.MenuGroups.Risk,   2, "shield-check",        "/information-security"),
        new(SeedIds.Modules.Quality,    "QUALITY",    "Kalite yönetimi",            "Quality Management",       SeedIds.MenuGroups.Risk,   3, "award",              "/quality"),
        new(SeedIds.Modules.Ohs,        "OHS",        "İş sağlığı güvenliği",       "Occupational Health & Safety", SeedIds.MenuGroups.Risk, 4, "hard-hat",         "/ohs"),
        // Hazır değil: menüde "yakında", lisanslanmaz.
        new(SeedIds.Modules.Env,        "ENV",        "Çevre",                      "Environment",              SeedIds.MenuGroups.Risk,   5, "leaf",               "/environment",  IsAvailable: false),
        new(SeedIds.Modules.Food,       "FOOD",       "Gıda güvenliği",             "Food Safety",              SeedIds.MenuGroups.Risk,   6, "utensils",          "/food-safety",  IsAvailable: false),

        // Sistem
        new(SeedIds.Modules.Reports,    "REPORTS",    "Raporlar",                   "Reports",                  SeedIds.MenuGroups.System, 1, "chart-column",          "/reports"),
        new(SeedIds.Modules.Users,      "USERS",      "Kullanıcılar",               "Users",                    SeedIds.MenuGroups.System, 2, "users",             "/users"),
        new(SeedIds.Modules.Defs,       "DEFS",       "Tanımlar",                   "Definitions",              SeedIds.MenuGroups.System, 3, "sliders-horizontal",            "/definitions"),
    ];
}
