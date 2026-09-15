# Yeni Quami — Yapım Durumu

Son güncelleme: 15 Eylül 2026
Son commit: `b3fc64a` iskelet: çözüm yapısı ve proje referansları

Bu dosya oturumlar arası devir içindir. Yeni oturum önce bunu okur, kaldığı
yerden devam eder. Her adım bitince güncellenir.

## Ortam

- .NET SDK 10.0.401 (`/usr/local/share/dotnet`)
- PostgreSQL 18.6, Homebrew, `brew services` ile çalışıyor, yerel bağlantı trust,
  `psql` yolu `~/.zshrc` içinde. Veritabanı `quami_dev` HENÜZ oluşturulmadı (adım 4).
- Docker yok.

## İskelet adımları

| # | Adım | Durum |
|---|---|---|
| 1 | Çözüm ve proje iskeleti, .gitignore, README | BİTTİ |
| 2 | Domain sınıfları: Tenant, Module, MenuGroup, TenantModule, AuditLog, User | BİTTİ (commit bekliyor) |
| 3 | DbContext, global query filter, SaveChanges override (TenantId + audit log) | BİTTİ (commit bekliyor) |
| 4 | İlk migration ve veritabanı oluşturma | SIRADA |
| 5 | Seed verisi: örnek kiracı, menü grupları, modül kayıtları | bekliyor |
| 6 | Kimlik ve oturum, ITenantContext | bekliyor |
| 7 | Blazor yerleşimi: akordeon menü (veriden), dil değiştirici, tema | bekliyor |
| 8 | Boş dashboard sayfası | bekliyor |
| 9 | IFileStorage + Quami.Api iskeleti | bekliyor |
| 10 | İlk commit | bekliyor (adım 1 ayrıca commit edildi) |

Çalışma kuralı: tek adım, tek komut; adım bitince Bülent'e rapor, onay, sonraki adım.
Şartnamenin tamamı bu oturumun iskelet prompt'unda; özeti README.md'de.

## Adım 1'de verilen şablon kararları

- **Blazor Web App şablonu**, `--interactivity Server --all-interactive --auth None`.
  .NET 10'da Blazor Server'ın karşılığı budur. Şablonun örnek sayfaları (Counter,
  Weather) duruyor; adım 7'de yerleşim kurulurken silinecek.
- **Minimal API şablonu** (`dotnet new webapi`, denetleyici yok). Örnek hava durumu
  ucu adım 9'da temizlenecek.
- **Directory.Build.props**: hedef çerçeve, Nullable, ImplicitUsings tek yerden.
  Şartnamede yoktu, eklendi. Proje dosyalarına bu ayarları tekrar yazmayın.
- **Klasik `.sln`**: .NET 10 varsayılanı `.slnx` idi; şartname `Quami.sln` dediği
  için `--format sln` ile klasik biçim kullanıldı.
- Kimlik doğrulama şablondan alınmadı; adım 6'da `IAuthenticationProvider`
  soyutlamasıyla elle kurulacak.
- Tests projesi xunit; Domain, Application, Infrastructure'a referanslı.

## Adım 2 için Bülent'in notları (teyitli, 15 Eylül 2026)

Ortak taban sınıf alanları:
- Id
- TenantId
- LegacyId (nullable string, eski Quami kaydının kimliği)
- OluşturmaTarihi
- OluşturanKullanıcıId
- GüncellemeTarihi
- GüncelleyenKullanıcıId
- SilindiMi

Silme mantıksal olacak (soft delete), global query filter silinmiş kayıtları
otomatik gizleyecek. ISO denetimlerinde kayıt fiziksel silinmemeli.

## Adım 2'de verilen kararlar

- **Kod dili İngilizce, açıklamalar Türkçe.** Şartnamedeki Türkçe alan adları
  C# özelliklerine İngilizce çevrildi (OluşturmaTarihi → CreatedAt,
  SilindiMi → IsDeleted, KısaAd → ShortName). Sebep: Türkçe karakterli
  tanımlayıcılar EF Core migration ve PostgreSQL sütun adlarında sorun çıkarır.
  Arayüzde görünen tüm metinler kaynak dosyalarından Türkçe/İngilizce gelir.
- **İki taban sınıf:** `BaseEntity` (Id, LegacyId, denetim alanları, soft delete)
  ve ondan türeyen `TenantEntity` (+TenantId). Tenant, Module, MenuGroup sistem
  geneli olduğu için TenantId taşıyamaz, doğrudan BaseEntity'den türer. İş
  modüllerinin tüm varlıkları TenantEntity'den türeyecek.
- Taban sınıftaki alanlar Bülent'in listesine ek olarak DeletedAt ve
  DeletedByUserId içerir (kim, ne zaman sildi; ISO izlenebilirliği).
- Üç arayüz: `IAuditable`, `ISoftDeletable`, `ITenantScoped`. Adım 3'teki
  SaveChanges override ve global query filter varlıkları bu arayüzlerle tanır.
- **Id türü Guid v7** (`Guid.CreateVersion7()`): sıralı, PostgreSQL indeksi dağılmaz.
- **AuditLog BaseEntity'den türemez:** salt ekleme tablosu, kendisi denetlenmez
  ve silinmez. Id `long`, TenantId nullable (sistem geneli kayıtlar için).
- **User Domain'de yalın:** parola/kilit/2FA yok. Adım 6'da Infrastructure'daki
  Identity kullanıcısı aynı Id ile eşlenir. Dil tercihi `Language` enum'u (Tr/En).
- **Module:** şartnamedeki MenüGrubuKodu yerine `MenuGroupId` FK + `MenuGroup`
  navigasyonu. Ek alanlar: `Route` (menü adresi) ve `IsAvailable` (ENV/FOOD
  "yakında" için false; lisanslanamaz).
- **TenantModule:** `IsEffectiveAt(utcNow)` kuralı: aktif + tarih aralığı içinde.
  (TenantId, ModuleId) benzersiz indeksi adım 3'te.
- Tarihler UTC `DateTime` (Npgsql timestamptz gereği).

## Adım 3 öncesi Bülent'in eklemeleri (uygulandı)

1. User TenantEntity'den türer, `IsSystemAdmin` alanı var. Muafiyet
   `ITenantContext.IsSystemAdmin` üzerinden query filter'da uygulanır.
2. Tüm benzersiz indeksler kısmi: `WHERE is_deleted = false`.
3. Sütun adları snake_case: EFCore.NamingConventions,
   `UseSnakeCaseNamingConvention()`. C# PascalCase kalır.

## Adım 3'te üretilenler ve kararlar

- `Quami.Application/Abstractions/ITenantContext.cs`: TenantId?, UserId?,
  IsSystemAdmin. Uygulaması adım 6'da (oturumdan beslenir). Şartname bunu adım
  6'da sayıyordu, filtre için arayüz şimdi gerekti.
- `Quami.Infrastructure/Persistence/QuamiDbContext.cs`:
  - EF Core 10 **adlandırılmış query filter**: "SoftDelete" ve "Tenant". Gerekince
    biri tek başına kapatılabilir: `IgnoreQueryFilters([QuamiDbContext.TenantFilter])`.
  - Kiracı filtresi: `IsSystemAdmin || (TenantId != null && e.TenantId == TenantId)`.
    Oturum yoksa hiç satır dönmez (güvenli varsayılan).
  - SaveChanges/SaveChangesAsync override: Added → TenantId (boşsa
    ITenantContext'ten, o da yoksa hata) + CreatedAt/By; Modified → UpdatedAt/By,
    Created* korunur; Deleted + ISoftDeletable → Modified'a çevrilir, yalnızca
    silme/güncelleme sütunları yazılır.
  - AuditLog: ana kayıtla AYNI SaveChanges'e eklenir, tek işlemde (transaction)
    yazılır; ya ikisi birlikte ya hiçbiri (Bülent'in düzeltmesi, adım 3 sonu).
    Insert'te Id Guid v7 istemci tarafında üretildiği için RecordId önceden bilinir.
    Dönüş değeri günlük satırlarını saymaz.
    Update'te yalnızca değişen alanlar; enum'lar JSON'da metin.
- `Persistence/Configurations/`: her varlık için IEntityTypeConfiguration.
  `BaseEntityConfiguration<T>` ortak eşleme + `HasActiveUniqueIndex` yardımcısı
  (kısmi benzersiz indeks). LegacyId'ye normal indeks.
- Enum'lar veritabanında metin (`HasConversion<string>()`): Language, Operation.
- AuditLog: Id identity (long), OldValues/NewValues `jsonb`, indeksler
  (table_name, record_id) ve (tenant_id, timestamp).
- FK'ler `DeleteBehavior.Restrict` (fiziksel silme zaten yok).
- `Quami.Infrastructure/DependencyInjection.cs`: `AddInfrastructure(services,
  configuration)`; bağlantı dizesi adı `ConnectionStrings:QuamiDb`. Web/Api
  Program.cs bağlantısı adım 4'te.
- Paketler: Npgsql.EntityFrameworkCore.PostgreSQL 10.0.3, EFCore.NamingConventions
  10.0.1, Microsoft.EntityFrameworkCore(.Relational/.Design) 10.0.12. Çekirdek ve
  Relational, Npgsql'in getirdiği 10.0.4 ile Design'ın 10.0.12'si çakıştığı için
  açıkça sabitlendi.
- Adım 4 için not: migration üretimi ITenantContext ister; tasarım zamanı için
  `IDesignTimeDbContextFactory` veya boş bir NullTenantContext gerekecek.

## Açık kararlar (Bülent'e ait)

- Modül içleri birebir mi taşınsın, arayüz de tazelensin mi (iş kuralı sabit)?
  Varsayım: iş kuralı birebir, arayüz ortak temayla tazelenir.
- "Süreç ve kayıt" grubu iki modülle kalsın mı? Varsayım: kalır.
- Çok kiracılılıkta satır seviyesi ayrım şimdilik; ayrı şema/veritabanı ileride.
