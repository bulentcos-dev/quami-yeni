# Yeni Quami — Yapım Durumu

Son güncelleme: 17 Eylül 2026
Son commit: ortak bileşenler ve lucide ikonları (bkz. `git log`)

Bu dosya oturumlar arası devir içindir. Yeni oturum önce bunu okur, kaldığı
yerden devam eder. Her adım bitince güncellenir.

## Ortam

- .NET SDK 10.0.401 (`/usr/local/share/dotnet`)
- PostgreSQL 18.6, Homebrew, `brew services` ile çalışıyor, yerel bağlantı trust,
  `psql` yolu `~/.zshrc` içinde. Veritabanı `quami_dev` oluşturuldu (adım 4), 7 tablo.
- Docker yok.

## İskelet adımları

| # | Adım | Durum |
|---|---|---|
| 1 | Çözüm ve proje iskeleti, .gitignore, README | BİTTİ |
| 2 | Domain sınıfları: Tenant, Module, MenuGroup, TenantModule, AuditLog, User | BİTTİ |
| 3 | DbContext, global query filter, SaveChanges override (TenantId + audit log) | BİTTİ |
| 4 | İlk migration ve veritabanı oluşturma | BİTTİ |
| 5 | Seed verisi: örnek kiracı, menü grupları, modül kayıtları | BİTTİ |
| 6 | Kimlik ve oturum, ITenantContext, kimlik olay günlüğü | BİTTİ |
| 7 | Blazor yerleşimi: akordeon menü (veriden), dil değiştirici, tema | BİTTİ |
| 7b | İkonlar (Lucide) ve ortak bileşen kümesi | BİTTİ |
| 8 | Boş dashboard sayfası | BİTTİ (commit bekliyor) |
| 9 | IFileStorage + Quami.Api iskeleti | SIRADA |
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

## Adım 4'te üretilenler ve kararlar

- `dotnet-ef` 10.0.12 yerel araç olarak `.config/dotnet-tools.json` içinde
  (global değil; yeni makinede `dotnet tool restore`).
- `Persistence/DesignTimeTenantContext.cs`: boş bağlam (kiracı yok, muafiyet yok).
  Migration üretimi ve ileride arka plan işleri için. İstek hattında kullanılmaz.
- `Persistence/QuamiDbContextFactory.cs`: IDesignTimeDbContextFactory. Bağlantı
  `QUAMI_DB` ortam değişkeninden, yoksa yerel varsayılan
  `Host=localhost;Port=5432;Database=quami_dev;Username=<kullanici>` (trust).
- Migration: `Persistence/Migrations/20260915130601_InitialCreate`. Veritabanı
  `quami_dev` `dotnet ef database update` ile oluştu. Tablolar: tenants,
  menu_groups, modules, tenant_modules, users, audit_logs, __EFMigrationsHistory.
  Kısmi benzersiz indeksler (`WHERE is_deleted = false`) ve jsonb sütunlar doğrulandı.
- Web: `appsettings.Development.json` içine `ConnectionStrings:QuamiDb`,
  `Program.cs` içine `AddInfrastructure`. ITenantContext henüz kayıtlı değil;
  DbContext çözülürse hata verir. Adım 5 seed'i için geçici kayıt gerekecek,
  kalıcı uygulama adım 6'da.
## Adım 5 için Bülent'in notları (teyitli, 16 Eylül 2026)

1. Menü grupları ve modüller için kimlikler kodda elle yazılmış sabit Guid
   değerleri olsun, her ortamda aynı. Eşleştirme kimlik üzerinden yapılsın, ad
   üzerinden değil — ad değişebilir.
2. Seed her açılışta çalışsın: sistem geneli kayıt yoksa ekle, varsa tanım
   alanlarını (AdTR, AdEN, Sıra, İkon, Route, IsAvailable) koda göre GÜNCELLE.
   Yani modül adını kodda değiştirdiğimde açılışta veritabanına yansısın.
   Kiracıya ait veriye (TenantModules aktiflik durumu, tarih aralığı) hiç
   dokunma — müşteri lisansını elle kapatmışsa açılışta geri açılmamalı.
3. Örnek kiracıya hazır modüllerin hepsi açık olsun. ENV ve FOOD
   IsAvailable = false olduğu için lisans kaydı açılmasın; lisans servisi hazır
   olmayan modülü hiçbir kiracı için etkin saymasın.

## Adım 5'te üretilenler ve kararlar

- `Persistence/Seed/SeedIds.cs`: elle yazılmış sabit Guid'ler. Menü grupları
  `1000...-00NN`, modüller `2000...-00NN`, örnek kiracı `3000...`, örnek
  kullanıcı `4000...`. **Bir değer canlıya çıktıktan sonra asla değişmez;**
  yeni modül yeni numara alır.
- `Persistence/Seed/SeedData.cs`: menünün tek kaynağı. 6 grup, 23 modül
  (ad TR/EN, sıra, ikon, route, IsAvailable). Burada yapılan değişiklik bir
  sonraki açılışta veritabanına yansır.
- `Persistence/Seed/DatabaseSeeder.cs`: `MigrateAndSeedAsync`.
  Sistem geneli kayıtlar kimlik üzerinden upsert edilir; silinmiş işaretliyse
  canlandırılır (kimlik birincil anahtarı işgal ettiği için yeniden ekleme
  çakışırdı). Kiracı verisine yalnızca hiç kiracı yokken dokunulur.
- `Services/ModuleLicenseService.cs` + `Application/Abstractions/IModuleLicenseService.cs`:
  `IsEnabledAsync`, `GetEnabledModulesAsync`. Üç koşul birlikte: modül hazır
  (IsAvailable), lisans aktif, tarih aralığı içinde. Sorgu kiracı süzgecini yok
  sayıp kiracıyı parametreden alır (sistem yöneticisi başka kiracıyı da
  sorgulayabilsin); silinmiş kayıt süzgeci açık kalır. Saat `TimeProvider`
  üzerinden (test edilebilir).
- `DesignTimeTenantContext` → **`NullTenantContext`** olarak yeniden adlandırıldı;
  hem migration üretimi hem açılış seed'i kullanıyor, "tasarım zamanı" adı
  yanıltıcıydı. `AddInfrastructure` içinde `TryAddScoped` ile geçici kayıtlı;
  adım 6'da gerçek uygulama eklenince devre dışı kalır.
- Migration yalnızca Development'ta açılışta uygulanır
  (`app.Environment.IsDevelopment()`); test/canlıda dağıtım adımı uygular.
- HOME grubu altında tek bir `HOME` modülü (route `/`) var. Adım 7'de menü
  bunu grup başlığı yerine doğrudan bağlantı olarak çizecek. Bülent onaylamazsa
  seed'den çıkarmak tek satır.
- Örnek kullanıcı `admin` (DEMO kiracısı) `IsSystemAdmin = false` ile
  oluşturulur. Parola yok; kimlik adım 6'da. Sistem yöneticisi hesabı bilinçli
  olarak seed'lenmedi — muafiyetli hesap güvenlik açısından elle açılmalı.
- **Seed kilidi (Bülent'in isteğiyle eklendi):** seed tek işlemde çalışır ve
  başında `pg_advisory_xact_lock` alınır (anahtar `0x5175616D69536564`, ASCII
  "QuamiSed", `DatabaseSeeder.SeedAdvisoryLockKey` — değiştirilmemeli). İkinci
  örnek kilidi bekler, kilit düşünce işin yapılmış olduğunu görüp değişiklik
  yazmadan geçer. Kilit işlem bitince kendiliğinden bırakılır, uygulama çökse
  bile sızmaz. Migration'ın kendi kilidi EF Core'da, ayrı.
  Sınandı: dışarıdan psql ile kilit tutulurken uygulama bekledi ("Seed kilidi
  bekleniyor"), kilit bırakılınca 0 değişiklikle geçti, artık kilit kalmadı.

### Adım 5 doğrulaması (gerçek veritabanında koşturuldu)

- Sayımlar: 6 grup, 23 modül, 21 lisans (23 − 2 hazır olmayan), 1 kiracı,
  1 kullanıcı. ENV ve FOOD için lisans kaydı yok.
- Veritabanı elle bozuldu (DOCS adı ve sırası değiştirildi, TASKS lisansı
  kapatıldı, kiracı adı değiştirildi) ve uygulama yeniden başlatıldı:
  DOCS koda göre düzeltildi; TASKS lisansı kapalı kaldı; kiracı adı
  korundu. Denetim günlüğü yalnızca 1 satır büyüdü — değişmeyen kayıt
  yazılmıyor.
- Lisans servisi gerçek sorguyla sınandı: DOCS true, TASKS (elle kapatılmış)
  false, ENV (hazır değil) false, olmayan kod false, etkin modül sayısı 20.
  Sınama sonrası kiracı verisi elle eski haline getirildi.

## Adım 6'da üretilenler ve kararlar

### Kimlik
- `Infrastructure/Identity/QuamiIdentityUser.cs`, `QuamiIdentityRole.cs`:
  ASP.NET Core Identity kayıtları. Domain'deki `User` ile **aynı Id**yi paylaşır;
  parola ve kilitlenme burada, iş bilgisi orada.
- **Giriş adı `kullanıcıadı@KURUMKODU`** (ör. `admin@DEMO`). Sebep: Identity
  kullanıcı adı genel olarak benzersiz olmalı, bizimki yalnızca kiracı içinde
  benzersiz. Giriş ekranı üç alan ister: kurum kodu, kullanıcı adı, parola.
  Tek alanlı (e-posta ile) girişe geçilecekse burası değişir.
- `QuamiDbContext` artık `IdentityDbContext<...>`. Identity tabloları elle
  snake_case adlara eşlendi (`identity_users`, `identity_roles`, ...): Identity
  kendi tablo adlarını açıkça verdiği için adlandırma kuralı onlara işlemiyordu.
- **Kendi kullanıcı kümemizin adı `BusinessUsers` oldu.** `Users` adı Identity'nin
  kendi kümesiyle çakışıyor ve onu gizliyordu (CS0114). Tablo adı `users` olarak
  sabitlendi, veritabanı değişmedi.
- **Identity varlıkları denetim günlüğüne yazılmaz.** Denetim yalnızca
  `BaseEntity` türevlerini kapsar; aksi halde parola özeti ve güvenlik damgası
  JSON olarak `audit_logs` içine düşerdi. Sınandı: günlükte identity tablosu yok,
  "password" geçen satır yok.
  Kimlik olayları ayrı bir günlüğe yazılır (aşağıya bakın).

### Soyutlama
- `Application/Abstractions/IAuthenticationProvider.cs` + `SignInStatus`:
  giriş ekranı yalnızca bunu bilir. Bugünkü uygulama `LocalAuthenticationProvider`
  (kullanıcı adı/parola). SAML ve Entra ID ileride ikinci ve üçüncü uygulama
  olarak eklenecek, ekran değişmeyecek.
- Parola kontrolünden önce iş tarafı kontrol edilir: kurum aktif mi, kullanıcı
  aktif mi. Hata mesajı hangi alanın yanlış olduğunu söylemez.

### Oturum ve kiracı bağlamı
- `Identity/CurrentUserTenantContext.cs`: `ITenantContext`in gerçek uygulaması.
  Kiracı ve yetki bilgisini oturum biletindeki taleplerden okur. Bilet
  `QuamiUserClaimsPrincipalFactory` ile üretilir; `quami:tenant_id` ve
  `quami:is_system_admin` talepleri Domain kullanıcısından her seferinde tazelenir.
- Oturum sahibi iki kaynaktan aranır: `IHttpContextAccessor` (istek hattı) ve
  `AuthenticationStateProvider` (Blazor devresi). Devre içinde HttpContext yoktur,
  bu yüzden ikisi de gerekli.
- Çerez: `quami.auth`, HttpOnly, SameSite=Lax, 8 saat, kayan süre.
  `returnUrl` parametre adı giriş ekranıyla hizalandı.

### Ekranlar ve yetkilendirme
- `Components/Public/` — oturum İSTEMEYEN sayfalar: Login, Logout, Error, NotFound.
- `Components/Pages/` — oturum İSTEYEN sayfalar. `Pages/_Imports.razor` içindeki
  `@attribute [Authorize]` sayesinde **yeni modül sayfası otomatik korumalı**;
  tek tek yazmak gerekmez, unutulamaz.
- Giriş ve çıkış sayfaları `[ExcludeFromInteractiveRouting]` ile statik render
  edilir: oturum çerezi yalnızca HTTP yanıtında yazılabilir, etkileşimli devrede
  yazılamaz. `App.razor` render kipini sayfaya göre seçer.
- Çıkış POST ile yapılır (GET bağlantısı başka siteden tetiklenebilirdi).
- Giriş sonrası dönüş adresi yalnızca site içi olabilir (açık yönlendirme koruması).

### Geliştirme hesabı
- Boş veritabanında `DEMO` kurumu, `admin` kullanıcısı ve **yalnızca
  Development'ta** bir parola oluşur (`<GELISTIRME-PAROLASI-KALDIRILDI>`, ayar:
  `Seed:SampleUserPassword`). Test/canlıda parola verilmez, hesap açılmaz.
  Açılışta uyarı günlüğü basılır. Bkz. README.
- Parola kuralı: en az 10 karakter, büyük/küçük harf, rakam, özel karakter.
  Kilit: 5 hatalı denemede 15 dakika.

### Kimlik olay günlüğü (Bülent'in isteğiyle, adım 6 sonunda eklendi)

- `Domain/Entities/AuthEvent.cs` → `auth_events` tablosu. Denetim günlüğünden
  **bağımsız**, salt ekleme: güncellenmez, silinmez, mantıksal silme yoktur.
  Denetim günlüğü veri değişikliğini izler, bu kimlik olayını.
- Kaydedilen olaylar (`AuthEventType`): `SignInSucceeded`, `SignInFailed`,
  `SignedOut`, `AccountLockedOut`, `PasswordChanged`.
- Başarısızlık sebepleri (`AuthFailureReason`): `TenantNotFound`,
  `TenantInactive`, `UserNotFound`, `UserInactive`, `InvalidPassword`,
  `LockedOut`, `RequiresTwoFactor`, `PasswordChangeRejected`.
- Her kayıtta: zaman (UTC), kurum kodu (yazıldığı gibi), denenen kullanıcı adı,
  TenantId ve UserId (bilinebiliyorsa), IP adresi, tarayıcı bilgisi, sonuç.
- **Parola veya parola özeti hiçbir alana yazılmaz.** Günlüğe parola hiç
  geçmez: `IAuthEventLog` arayüzünde parola alanı yoktur.
- **Ekran mesajı ayrım yapmaz.** "Kurum yok", "kullanıcı yok" ve "parola yanlış"
  ekranda aynı cümleyi verir; ayrım yalnızca günlüktedir. Kullanıcı adı taraması
  böylece engellenir. (Pasif kurum/kullanıcı ayrı bir mesaj verir — bilinçli,
  destek kolaylığı için; istenirse o da tek mesaja indirilebilir.)
- Eleme sırası kurum → kullanıcı → parola olacak şekilde yeniden kuruldu.
  Yan fayda: kurum kodu artık büyük/küçük harf duyarsız (`demo` = `DEMO`),
  günlüğe kurumun kayıtlı yazımı düşer.
- `IAuthenticationProvider.ChangePasswordAsync` eklendi. Parola değiştirme
  ekranı henüz yok; ekran yapıldığında bu yöntemi çağıracak ve olay
  kendiliğinden günlüğe düşecek. Yol sınandı (başarılı ve reddedilen değişim).
- IP adresi `RemoteIpAddress`ten okunur. Ters vekil (reverse proxy) arkasında
  gerçek adresi görmek için `ForwardedHeaders` ara yazılımı yapılandırılmalı.
  Test sunucusuna çıkarken bakılacak.

### Adım 6 doğrulaması (curl ile, gerçek sunucuda)
- Oturumsuz `/` ve `/counter` → `/login?returnUrl=...` yönlendirmesi.
- Yanlış parola → "Kurum kodu, kullanıcı adı veya parola hatalı." mesajı.
- Doğru parola → 302 ve `quami.auth` çerezi; korumalı sayfa açılıyor.
- Antiforgery belirteci olmadan giriş POST'u → 400.
- Çıkış → çerez silindi, korumalı uç yeniden giriş istiyor.
- Kiracı bağlamı: talepler doğru okundu; süzgeç elle filtre yazılmadan yalnızca
  kendi kiracısının verisini döndürdü (1 kullanıcı, 21 lisans, 21 etkin modül).
- `last_login_at` yazıldı; denetim günlüğüne parola sızmadı.
- **Blazor devresi içinde** (tarayıcıyla, geçici bir sayfa ile): `etkilesimli=True`
  iken kiracı, kullanıcı ve yetki doğru okundu, lisans servisi 21 modül döndürdü.
  Yani kiracı bağlamı hem istek hattında hem devrede çalışıyor; adım 7'deki menü
  veriyi görecek. Geçici sayfa kaldırıldı.
- Depoya `.gitattributes` eklendi (`* text=auto`). Şablon dosyaları CRLF, yeni
  dosyalar LF olduğu için diff'ler gereksiz büyüyordu.

### Kimlik olay günlüğü doğrulaması (curl ile)

- Yanlış kurum kodu, yanlış kullanıcı ve yanlış parola: ekranda ÜÇÜ DE aynı
  mesaj; günlükte `TenantNotFound`, `UserNotFound`, `InvalidPassword` olarak
  ayrı ayrı. IP ve tarayıcı bilgisi her kayıtta dolu.
- Başarılı giriş, çıkış kaydedildi. Küçük harfle (`demo`) girişte günlüğe
  kurumun kayıtlı yazımı (`DEMO`) düştü.
- Art arda 5 hatalı deneme: 5. denemede `AccountLockedOut` ve ayrıca
  `SignInFailed`/`LockedOut` kaydı. Sınama sonrası hesabın kilidi açıldı.
- Parola değişikliği: reddedilen deneme `PasswordChangeRejected` ile, başarılı
  iki değişim sebep alanı boş olarak kaydedildi. Parola sınama sonunda eski
  haline döndürüldü.
- Tabloda parola geçen kayıt yok; `auth_events` denetim günlüğüne yazılmıyor.
- Sınama kayıtları geliştirme veritabanında duruyor (salt ekleme tablo, silinmez).

## Adım 7'de üretilenler ve kararlar

### Menü (tamamen veriden)
- `Application/Menu/` → `IMenuService`, `MenuGroupView`, `MenuItemView`, `ModuleAccess`.
  Uygulaması `Infrastructure/Services/MenuService.cs`.
- Menüde koda gömülü tek satır yok. Başlıklar, sıralar, adresler, adlar hep
  `menu_groups` / `modules` tablolarından; hangi modülün görüneceği kiracının
  lisansından.
- Görünürlük kuralı: hazır + lisanslı → tıklanabilir; hazır değil (ENV, FOOD) →
  soluk, "(yakında)", tıklanmaz; hazır ama lisanssız → menüde HİÇ yok.
- **`MenuGroup.IsDirectLink` sütunu eklendi** (migration `MenuGroupDirectLink`).
  HOME için true: menüde açılır başlık değil, doğrudan bağlantı olarak çizilir.
  Davranış koda değil veriye bağlı; başka bir grup da böyle yapılabilir.
- Akordeon: açılışta yalnız ana başlıklar, tek seferde tek grup açık. Bulunulan
  sayfanın grubu kendiliğinden açılır.

### Lisans kontrolü (sunucuda)
- `Components/ModuleAccessGuard.razor`, MainLayout içinde `@Body`yi sarar.
  Adres bir modüle aitse `IMenuService.CheckRouteAsync` ile bakılır; lisanssız
  veya hazır değilse sayfa HİÇ oluşturulmaz, yerine açıklama panosu gelir.
  Blazor Server'da bu kod sunucuda çalışır: adres elle yazılsa da açılmaz.
- Modüle ait olmayan adresler (giriş, hata) serbesttir.

### Dil
- `Resources/SharedResource.resx` (Türkçe, varsayılan) ve `.en.resx`. 36 anahtar.
  Ekranda görünen her metin buradan gelir; menü adları veritabanından.
- **İşaretçi sınıf `SharedResource` projenin KÖK ad alanında** (`Quami.Web`).
  `Quami.Web.Resources` içinde olsaydı aranan yol `Resources/Resources.SharedResource.resx`
  olurdu ve kaynaklar bulunamazdı (bu hata yaşandı ve düzeltildi).
- Kültür sırası: çerez (açık seçim) → kullanıcının kayıtlı dili
  (`quami:language` talebi, `UserClaimCultureProvider`). Tarayıcının
  Accept-Language başlığı bilerek kullanılmıyor; varsayılan Türkçe.
- `/culture?lang=..&returnUrl=..` ucu çerezi yazar ve oturum açıksa
  `users.language` alanını günceller. Tam sayfa yüklemesi gerekir: kültür
  Blazor devresi kurulurken belirlenir. Açık yönlendirme koruması var.
- Doğrulama mesajları DataAnnotations kaynak bağlama ile YAZILMADI: anahtarlarda
  nokta var ve `SharedResource` üretilmiş tasarımcı sınıfı değil; o yol çalışma
  anında patlardı. Zorunlu alan kontrolü kodda, metin yine kaynak dosyasından.

### Tema
- Tüm renkler `wwwroot/app.css` içinde CSS değişkeni: `--quami-primary` #1E3556,
  `--quami-accent` #E8772E ve türevleri. Sayfa/bileşen içine renk yazılmıyor.
- **Bootstrap kaldırıldı** (`wwwroot/lib` silindi, App.razor'daki bağlantı çıktı).
  Şablonun `MainLayout.razor.css` ve `NavMenu.razor.css` dosyaları da silindi:
  tema tek dosyada olsun.
- **İkonlar henüz çizilmiyor.** Veride ikon adları duruyor (Bootstrap Icons
  adlandırması) ama bir ikon seti seçilmediği için glif basılmıyor. Set
  seçilince yalnızca menü şablonuna eklenecek, veri hazır.

### DbContext fabrikası (yol boyunca çıkan gerçek hata)
- Blazor Server'da bir devre içinde NavMenu ile ModuleAccessGuard aynı anda
  sorgu çalıştırınca paylaşılan DbContext patladı
  ("A second operation was started on this context instance...").
- Çözüm: `AddDbContextFactory<QuamiDbContext>(..., ServiceLifetime.Scoped)`.
  Okuma servisleri (`MenuService`, `ModuleLicenseService`) her çağrıda kendi
  kısa ömürlü bağlamını açar. Tek iş parçacıklı akışlar (giriş, seed, dil ucu)
  için kapsamlı `QuamiDbContext` kaydı fabrikadan üretilerek korundu.
- **Kural:** bundan sonra Blazor bileşenlerinden çağrılan okuma servisleri
  fabrikayı kullanmalı, doğrudan DbContext'i değil.

### Sayfalar
- `Components/Pages/ModulePlaceholder.razor`: 22 modül adresini karşılayan
  **geçici iskele**. Modülün adını menüden okuyup gösterir, "yapım sırasında"
  der. Bir modül yazıldığında adresi bu dosyadan çıkarılır; liste bitince dosya
  silinir.
- Şablondan gelen `Counter.razor` ve `Weather.razor` silindi.
- `Error.razor` ve `NotFound.razor` yerelleştirildi ve sade yerleşime alındı.
- Giriş/çıkış ekranları temaya ve kaynak dosyalarına taşındı.

### Adım 7 doğrulaması (tarayıcıda, gerçek sunucuda)
- Menü veriden geldi: 6 grup, Ana sayfa doğrudan bağlantı olarak.
- Akordeon tek-açık: Risk açıkken İşlerim'e tıklanınca Risk kapandı.
- Çevre ve Gıda güvenliği soluk ve "(yakında)" etiketiyle, tıklanmaz.
- Görevler'e gidildi: yer tutucu sayfa modül adını veriden gösterdi, bağlantı
  turuncu çizgiyle etkin işaretlendi, grup açık kaldı.
- Dil TR→EN: menü, sayfa başlığı, mesajlar ve "Sign out" İngilizceye döndü;
  açık grup ve etkin satır korundu. Tercih `users.language` alanına yazıldı.
- Lisans kapatma sınaması: TASKS lisansı kapatılınca Görevler menüden kayboldu
  ve `/tasks` adresi elle yazılınca "Bu modüle erişiminiz yok" panosu geldi.
  Sınama sonrası lisans geri açıldı.
- `/environment` elle yazılınca "Bu modül henüz hazır değil" panosu geldi.
- Oturum boyunca sunucu günlüğünde hata yok.

## Adım 7b: ikonlar ve ortak bileşen kümesi

### İkonlar — Lucide
- `wwwroot/lucide.svg`: 39 Lucide ikonu tek bir SVG sprite dosyasında, YEREL.
  Dış bağımlılık yok, çevrimdışı çalışır. Lucide ISC lisanslı; dosyanın başında
  kaynak notu var.
- Kullanım: `<QuamiIcon Name="house" Size="18" />`. Renk `currentColor`dan gelir,
  yani bulunduğu yerin metin rengini alır.
- Veri alanındaki ikon adları Lucide adlarına çevrildi (seed'de güncellendi,
  açılışta veritabanına yansıdı — adım 5 kuralı çalıştı).
  Örnek: `check2-square` → `square-check-big`, `bar-chart` → `chart-column`,
  `tree` → `leaf`, `gear` → `settings`.
- Menüde grup başlıkları, modül satırları ve "yakında" satırları ikonlu; akordeon
  oku da Lucide `chevron-right` (açılınca döner).

### Ortak bileşenler — `Components/Shared/`
Hepsi tema değişkenlerini kullanır. **Modül içine stil yazılmaz, bunlar kullanılır.**

| Bileşen | Ne yapar |
|---|---|
| `QuamiIcon` | Lucide ikonu |
| `QuamiButton` | Birincil / ikincil / tehlikeli; ikon, kapalı ve "işlem sürüyor" durumu |
| `QuamiPageHeader` | Sayfa başlığı + açıklama + sağda eylem butonları |
| `QuamiAlert` | Bilgi / başarı / uyarı / tehlike kutusu |
| `QuamiSpinner` | Yükleniyor göstergesi (satır içi veya blok) |
| `QuamiModal` | Genel pencere; Esc, çarpı, arka plan tıklamasıyla kapanır |
| `QuamiConfirmDialog` | Onay penceresi; onay butonu varsayılan olarak tehlikeli |
| `QuamiDataTable` + `QuamiColumn` | Sıralama, sayfalama, boş durum, yükleniyor durumu |
| `QuamiTextField` | Metin |
| `QuamiTextAreaField` | Çok satırlı metin |
| `QuamiNumberField` | Sayı (int/long/decimal/double ve nullable) |
| `QuamiDateField` | Tarih (DateTime/DateOnly, tarih+saat seçeneği) |
| `QuamiSelectField` | Açılır liste (`QuamiOption<T>`) |
| `QuamiCheckboxField` | Onay kutusu |
| `QuamiFieldShell` | Alanların ortak çerçevesi: etiket, zorunlu işareti, doğrulama, ipucu |

- Form alanları `@bind-Value` ile çalışır ve `EditForm` içinde doğrulama
  mesajını kendileri gösterir. `EditForm` dışında da kullanılabilirler
  (doğrulama bloğu o zaman çizilmez).
- Tablo sütunları `QuamiColumn` ile bildirilir; sütun kendini tabloya kaydeder,
  tablo çizer. `SortBy` verilen sütunun başlığı tıklanabilir olur.
- Yeni ortak metinler kaynak dosyalarına eklendi (boş tablo, sayfalama, onayla,
  vazgeç, kapat, seçiniz, yükleniyor...). Bileşenlerde gömülü metin yok.

### Örnek sayfa
- `/dev/components` → `Components/Pages/ComponentGallery.razor`.
  **Geliştirici sayfası:** menüde yok, bir modüle ait değil, oturum ister.
  Bütün bileşenlerin canlı örneği; modül yazarken buraya bakılır.

### Adım 7b doğrulaması (tarayıcıda)
- Menü ikonları çizildi (ev, liste, klasör, pano, kalkan, ayar).
- Dört uyarı türü, beş buton durumu, sayfa başlığı ve eylem butonları göründü.
- Tablo: sıralama (KOD'a iki tık → azalan, ok göstergesi), sayfalama
  (Sayfa 1/2 → 2/2, uçlarda butonlar kapanıyor), boş durum
  ("Gösterilecek kayıt yok."), yükleniyor durumu.
- Onay penceresi açıldı, "Sil" onayı çalıştı, pencere kapandı.
- Form doğrulaması: boş zorunlu alanla kaydetmeye çalışınca alanın altında
  kırmızı mesaj çıktı, form gönderilmedi.
- Sunucu günlüğünde hata yok.

## Adım 8'de üretilenler ve kararlar

Bu adımda **veri yok**: yalnızca yerleşim ve yer tutucular. Sayfaya özel stil
yazılmadı; her şey ortak bileşenler ve ortak yerleşim sınıflarıyla kuruldu.

- `Components/Pages/Home.razor` (adres `/`) gösterge paneli oldu. Sırayla:
  1. **Yapay zekâ öncelik şeridi** — `Components/Dashboard/DashboardAiStrip.razor`.
     Bu fazda `Show=false`, yani hiç çizilmez. Yeri yerleşimde ayrılmıştır:
     faz 2'de `Show="true"` verilecek, şerit en üste girecek, sayfanın gerisi
     olduğu gibi aşağı kayacak. Sınandı: şerit açıkken yerleşim bozulmadı.
  2. **Modül kartları** — kiracının etkin modüllerinden kurulur, koda gömülü
     liste yok. Her kart modülün ikonunu ve adını gösterir, tıklanınca modüle
     gider. Özet içeriği modül yazıldığında kartın içine gelecek.
     Ana sayfa kendi kartını göstermez.
  3. **İki grafik alanı** — düzeltici faaliyet 6 aylık akış (çizgi) ve risk
     seviyesi dağılımı (halka). Şimdilik yer tutucu kutu.
     **Grafik kütüphanesi seçilmedi**; grafikler gerçekten çizilirken konuşulacak.
  4. **Takvim** — ileride denetim/sözleşme/doküman/bakım tarihleriyle
     kendiliğinden dolacak, elle kayıt da eklenebilecek.
  5. **Duyurular** — elle girilecek.
- Yeni ortak bileşenler: `QuamiPlaceholder` (yer tutucu alan; kesik çerçeve,
  başlık, açıklama, istenirse en az yükseklik) ve `QuamiCard` (Href verilirse
  tıklanabilir kart).
- Yeni ortak yerleşim sınıfları (`app.css`): `.quami-stack`, `.quami-grid`,
  `.quami-grid--halves`, `.quami-grid--cards`. Bunlar sayfaya değil temaya aittir.
- Tüm metinler kaynak dosyalarında (14 yeni anahtar).

### Adım 8 doğrulaması (tarayıcıda)
- 20 modül kartı çizildi, her biri kendi Lucide ikonuyla.
- Karta tıklanınca modülün adresine gidildi.
- Dört yer tutucu alan (iki grafik, takvim, duyurular) göründü; kesik çerçeve
  onları gerçek kartlardan ayırıyor.
- Şerit geçici olarak açılıp kapatıldı: açıkken sayfa aşağı kaydı, yerleşim
  bozulmadı. Sonra gizli haline döndürüldü.
- Sunucu günlüğünde hata yok.

## Açık kararlar (Bülent'e ait)

- Modül içleri birebir mi taşınsın, arayüz de tazelensin mi (iş kuralı sabit)?
  Varsayım: iş kuralı birebir, arayüz ortak temayla tazelenir.
- "Süreç ve kayıt" grubu iki modülle kalsın mı? Varsayım: kalır.
- Çok kiracılılıkta satır seviyesi ayrım şimdilik; ayrı şema/veritabanı ileride.
