# Quami

Quami, kurumların yönetim sistemlerini tek yerden yürütmesi için yazılmış bir
uyum ve yönetim yazılımıdır: dokümanlar, sözleşmeler, denetimler, riskler,
düzeltici faaliyetler, eğitimler ve bakım kayıtları.

Bu depo **yeni Quami**'yi içerir: canlıda çalışan eski ürünün (ASP.NET Web Forms
ve SQL Server) aynı iş kurallarıyla .NET 10 üzerinde yeniden yazımı. Eski ürün
geçiş gününe kadar çalışmaya devam eder; yeni ürün onun yanında büyür.

Bu fazda **yapay zekâ yoktur.** Önce yapay zekâsız, tam çalışan bir ürün
çıkarılır. Yapay zekâ katmanı sonraki fazda ayrı bir Python servisi olarak
gelecek ve `Quami.Api` üzerinden bağlanacak; iskelet buna göre kurulmuştur.

---

## Teknolojiler

| Katman | Teknoloji |
|---|---|
| Çalışma zamanı | .NET 10 (uzun destekli sürüm), C# |
| Arayüz | ASP.NET Core + Blazor Server |
| Veritabanı | PostgreSQL 18, Entity Framework Core (Npgsql sağlayıcısı) |
| Kimlik | ASP.NET Core Identity, `IAuthenticationProvider` arkasında soyutlanmış |
| Diller | Türkçe (varsayılan) ve İngilizce, .NET yerleşik yerelleştirme |
| İkonlar | Lucide, yerel kopya |

Kimlik soyutlaması, ileride SAML ve Microsoft Entra ID ile çoklu oturum açmanın
ekranları değiştirmeden eklenebilmesi içindir.

---

## Sıfırdan kurulum

Aşağıdaki adımlar macOS içindir. Windows'ta paket yöneticisi dışındaki adımlar
aynıdır.

### 1. Araçlar

```bash
brew install --cask dotnet-sdk      # .NET SDK 10 (yönetici parolası sorar)
brew install postgresql@18
brew services start postgresql@18
```

`psql` komutunu kullanabilmek için kabuk ayarınıza ekleyin:

```bash
echo 'export PATH="/opt/homebrew/opt/postgresql@18/bin:$PATH"' >> ~/.zshrc
```

Kurulumu doğrulayın:

```bash
dotnet --version     # 10.x
psql -d postgres -c "select version();"
```

### 2. Ayarları verin

Depoda gerçek ayar dosyası yoktur; `appsettings.Development.json` dosyaları
bilerek depo dışındadır (bağlantı dizesi, parola ve API anahtarı taşırlar).
Her projenin yanında `.example` uzantılı bir şablon vardır.

```bash
cp src/Quami.Web/appsettings.Development.json.example src/Quami.Web/appsettings.Development.json
cp src/Quami.Api/appsettings.Development.json.example src/Quami.Api/appsettings.Development.json
```

Kopyaladığınız dosyalarda `KULLANICI_ADINIZ` yerine PostgreSQL kullanıcı adınızı
yazın (Homebrew kurulumunda bu genellikle makinedeki oturum adınızdır).

Parola ve API anahtarı gibi değerleri dosyaya yazmak yerine kullanıcı gizli
deposuna koymanız önerilir. Ayrıntılar aşağıdaki **Ayarlar** bölümünde.

### 3. Veritabanını oluşturun

```bash
dotnet tool restore
dotnet ef database update --project src/Quami.Infrastructure
```

`quami_dev` veritabanı yoksa oluşturulur ve tablolar kurulur.

### 4. Çalıştırın

```bash
dotnet run --project src/Quami.Web     # http://localhost:5046
dotnet run --project src/Quami.Api     # http://localhost:5250
```

Uygulama ilk açılışta örnek kurumu, menü gruplarını ve modül kayıtlarını
kendiliğinden oluşturur.

### 5. Geliştirme hesabı

Boş bir veritabanında `DEMO` adlı örnek kurum ve `admin` adlı kullanıcı
oluşturulur. Bu kullanıcının **parolası ayarlardan gelir; kodda varsayılan
parola yoktur.** Ayar verilmezse giriş kaydı açılmaz ve açılış günlüğünde bu
durum bildirilir.

Geliştirme parolasını kullanıcı gizli deposuna koyun:

```bash
dotnet user-secrets --project src/Quami.Web set "Seed:SampleUserPassword" "<güçlü bir geliştirme parolası>"
```

Parola kuralı: en az 10 karakter, büyük ve küçük harf, rakam, özel karakter.

Sonra giriş ekranında:

| Alan | Değer |
|---|---|
| Kurum kodu | `DEMO` |
| Kullanıcı adı | `admin` |
| Parola | ayarda verdiğiniz değer |

Bu hesap **yalnızca Development ortamında** açılır; test ve canlı ortamda
oluşturulmaz.

---

## Ayarlar

Uygulama ayarları üç kaynaktan okunur. Sonraki kaynak öncekini ezer:

1. `appsettings.json` — depoda, gizli hiçbir değer içermez.
2. `appsettings.Development.json` — **depoda değildir**, makineye özeldir.
3. Kullanıcı gizli deposu (user secrets) ve ortam değişkenleri.

Canlı ve test ortamlarında ayarlar ortam değişkenleriyle verilir; dosyaya
yazılmaz.

### Hangi ayar ne işe yarar

| Ayar | Ne işe yarar | Ortam değişkeni karşılığı |
|---|---|---|
| `ConnectionStrings:QuamiDb` | PostgreSQL bağlantısı | `ConnectionStrings__QuamiDb` |
| `Seed:SampleUserPassword` | Örnek `admin` kullanıcısının parolası. Yalnızca Development'ta kullanılır; boşsa hesap parolasız kalır. | `Seed__SampleUserPassword` |
| `Api:Keys:<istemci adı>` | `Quami.Api` uçlarının beklediği API anahtarı. Tanımlı değilse korumalı uçlar 401 döner. | `Api__Keys__<istemci adı>` |
| `FileStorage:RootPath` | Kullanıcı dosyalarının kök klasörü. Göreli verilirse uygulamanın içerik köküne göre çözülür. | `FileStorage__RootPath` |
| `FileStorage:DownloadPathPrefix` | İndirme adreslerinin yol öneki. | `FileStorage__DownloadPathPrefix` |

İç içe ayarlar ortam değişkeninde **iki alt çizgi** ile ayrılır (`:` yerine `__`).

### Kullanıcı gizli deposu (önerilen yol)

Gizli depo, değerleri projenin dışında, kullanıcı profilinizde tutar; depoya
sızma ihtimali yoktur. Yalnızca Development ortamında okunur.

```bash
# Veritabanı bağlantısı
dotnet user-secrets --project src/Quami.Web set "ConnectionStrings:QuamiDb" \
  "Host=localhost;Port=5432;Database=quami_dev;Username=<kullanıcı adınız>"

# Örnek kullanıcının parolası
dotnet user-secrets --project src/Quami.Web set "Seed:SampleUserPassword" "<parola>"

# API anahtarı (Quami.Api projesi için)
dotnet user-secrets --project src/Quami.Api set "ConnectionStrings:QuamiDb" \
  "Host=localhost;Port=5432;Database=quami_dev;Username=<kullanıcı adınız>"
dotnet user-secrets --project src/Quami.Api set "Api:Keys:python-ai" "<anahtar>"

# Ne tanımlı, görmek için
dotnet user-secrets --project src/Quami.Web list
```

### `dotnet ef` komutları için

Migration komutları web sunucusunu ayağa kaldırmaz, bu yüzden gizli depoyu
okumazlar. Bağlantıyı `QUAMI_DB` ortam değişkeninden alırlar:

```bash
export QUAMI_DB="Host=localhost;Port=5432;Database=quami_dev;Username=<kullanıcı adınız>"
dotnet ef database update --project src/Quami.Infrastructure
```

Değişken verilmezse yerel geliştirme varsayılanı kurulur: `localhost`,
`quami_dev` ve veritabanı kullanıcısı olarak makinedeki oturum adınız. Homebrew
ile kurulan PostgreSQL'de bu genellikle doğrudan çalışır.

### Gizli değerleri asla depoya yazmayın

- `appsettings.Development.json` dosyaları `.gitignore` içindedir; oradan
  çıkarmayın.
- Parola, API anahtarı veya bağlantı dizesini `appsettings.json`, kod veya
  belge dosyalarına yazmayın.
- Kodda hiçbir varsayılan parola veya anahtar yoktur; bu bilinçli bir karardır.

---

## Günlük komutlar

```bash
dotnet build                                    # tüm çözümü derle
dotnet run --project src/Quami.Web              # arayüzü çalıştır
dotnet test                                     # testleri çalıştır

# yeni migration üret
dotnet ef migrations add <Ad> --project src/Quami.Infrastructure --output-dir Persistence/Migrations

# migration'ları uygula
dotnet ef database update --project src/Quami.Infrastructure
```

Ortak bileşenlerin canlı örnekleri: uygulama çalışırken `/dev/components`.

---

## Klasör yapısı

```
Quami.sln
src/
  Quami.Domain/          varlıklar, enum'lar, iş kuralı arayüzleri
  Quami.Application/     servis arayüzleri, modeller, soyutlamalar
  Quami.Infrastructure/  veritabanı, kimlik, dosya deposu, servis uygulamaları
  Quami.Web/             Blazor Server arayüzü: sayfalar, bileşenler, tema
  Quami.Api/             REST arayüzü (yapay zekâ servisi ve Jira için)
tests/
  Quami.Tests/
docs/
```

Bağımlılık yönü: `Web` ve `Api` → `Infrastructure` → `Application` → `Domain`.
`Domain` hiçbir şeye bağlı değildir; iş kuralları orada, teknoloji dışarıda kalır.

| Klasör | Ne işe yarar |
|---|---|
| `Quami.Domain` | İş nesneleri ve kuralları. Veritabanı, web veya kütüphane bilgisi yoktur. |
| `Quami.Application` | Ekranların ve API'nin konuştuğu arayüzler. Uygulaması burada değildir. |
| `Quami.Infrastructure` | Bu arayüzlerin gerçek uygulamaları: Entity Framework Core, Identity, disk. |
| `Quami.Web` | Kullanıcının gördüğü her şey: sayfalar, ortak bileşenler, tema, metinler. |
| `Quami.Api` | Dış sistemlerin bağlanacağı uç. Bu fazda içi boş. |
| `docs` | Devir notları ve modül yazım rehberi. |

Önemli dosyalar:

| Dosya | İçerik |
|---|---|
| `src/Quami.Web/wwwroot/app.css` | Temanın tamamı. Renkler yalnızca burada tanımlıdır. |
| `src/Quami.Web/Resources/SharedResource*.resx` | Ekranda görünen bütün metinler. |
| `src/Quami.Infrastructure/Persistence/Seed/SeedData.cs` | Menünün kaynağı: gruplar ve modüller. |
| `src/Quami.Infrastructure/Persistence/Seed/SeedIds.cs` | Sabit kimlikler. Var olan değer değiştirilmez. |
| `docs/MODUL_YAZIM_REHBERI.md` | Yeni modül nasıl yazılır. |
| `docs/DURUM.md` | Nerede kalındı, hangi karar neden alındı. |

---

## Temel ilkeler

- **Çok kiracılılık:** tek veritabanı, her tabloda kiracı kimliği, süzgeç
  veritabanı bağlamında geneldir. Sorgularda elle kiracı filtresi yazılmaz.
- **Modül lisanslama:** kiracı için etkin olmayan modül menüde görünmez ve
  adresi elle yazılarak da açılamaz. Kontrol sunucudadır.
- **Menü veriden gelir:** başlıklar, sıralar, adlar ve adresler veritabanındadır,
  kodda menü yoktur.
- **Mantıksal silme:** kayıt fiziksel olarak silinmez, işaretlenir. Denetim izi
  korunur.
- **Denetim günlüğü:** her ekleme, güncelleme ve silme otomatik olarak
  `audit_logs` tablosuna yazılır.
- **Kimlik olay günlüğü:** giriş, çıkış, başarısız deneme, hesap kilitlenmesi ve
  parola değişikliği ayrı bir `auth_events` tablosuna yazılır. Parola veya parola
  özeti hiçbir günlüğe yazılmaz.
- **Tema tek yerde:** renkler CSS değişkenidir; lacivert `#1E3556`, turuncu
  `#E8772E`. Sayfa içine renk yazılmaz.
- **Dosya deposu:** `IFileStorage` arkasındadır. Dosyalar kiracıya göre ayrı
  klasörlerde durur, diskteki ad üretilir, gerçek ad veritabanında tutulur.
  Amazon S3'e geçiş tek bir uygulama değişikliğidir.
- **API anahtarı:** `Quami.Api` uçları `X-Api-Key` başlığı ister, anahtarlar
  `Api:Keys` ayarından gelir. Varsayılan anahtar yoktur; tanımlı değilse
  korumalı uçlar 401 döner.

---

## Katkı

Yeni bir modül yazmadan önce `docs/MODUL_YAZIM_REHBERI.md` dosyasını okuyun.
Modüller menü sırasıyla, birer birer yazılır: bir modül bitip test edilmeden
sonrakine geçilmez.
