# Quami

Yeni Quami: eski Quami'nin (ASP.NET Web Forms + SQL Server) aynı iş kurallarıyla
.NET 10 üzerinde yeniden yazımı. Bu depo yeni ürünü içerir; eski ürün D-Day'e
kadar canlıda çalışmaya devam eder.

## Teknoloji

| Katman | Teknoloji |
|---|---|
| Çalışma zamanı | .NET 10 (LTS), C# |
| Arayüz | ASP.NET Core + Blazor Server |
| Veritabanı | PostgreSQL 18, Entity Framework Core (Npgsql) |
| Kimlik | ASP.NET Core Identity, `IAuthenticationProvider` arkasında (ileride SAML / Entra ID) |
| Dil | TR / EN, .NET yerleşik localization |

## Çözüm yapısı

```
Quami.sln
src/
  Quami.Domain/          varlıklar, enum'lar, iş kuralı arayüzleri (dış bağımlılık yok)
  Quami.Application/     servis katmanı, modül arayüzleri, yetki kontrolü
  Quami.Infrastructure/  EF Core DbContext, migration'lar, depolama, dış servisler
  Quami.Web/             Blazor Server: sayfalar, bileşenler, tema
  Quami.Api/             REST API (yapay zeka servisi ve Jira entegrasyonu için)
tests/
  Quami.Tests/
docs/
```

Bağımlılık yönü: `Web`/`Api` → `Infrastructure` → `Application` → `Domain`.

## Temel ilkeler

- **Çok kiracılılık:** tek veritabanı, her tabloda `TenantId`, EF Core global query filter.
  Kiracı erişimi yalnızca `ITenantContext` üzerinden.
- **Modül lisanslama:** kiracı için aktif olmayan modül menüde görünmez ve adresle açılamaz.
- **Menü:** veriden beslenen akordeon (`MenuGroups`, `Modules`), kodda sabit değil.
- **Denetim günlüğü:** tüm ekleme/güncelleme/silme `AuditLog` tablosuna otomatik yazılır.
- **Tema:** renkler tek yerde CSS değişkeni olarak; lacivert `#1E3556`, turuncu `#E8772E`.
- **Bu fazda yapay zeka yok.** Faz 2'de ayrı Python servisi `Quami.Api` üzerinden bağlanır.

## Yerel geliştirme

Gereksinimler: .NET SDK 10, PostgreSQL 18 (yerelde çalışır durumda, veritabanı adı `quami_dev`).

```
dotnet tool restore
dotnet ef database update --project src/Quami.Infrastructure
dotnet build
dotnet run --project src/Quami.Web
```

Bağlantı dizesi `src/Quami.Web/appsettings.Development.json` içinde
(`ConnectionStrings:QuamiDb`). `dotnet ef` komutları için `QUAMI_DB` ortam
değişkeni verilebilir; yoksa yerel varsayılan kullanılır.

Yeni migration: `dotnet ef migrations add <Ad> --project src/Quami.Infrastructure --output-dir Persistence/Migrations`
