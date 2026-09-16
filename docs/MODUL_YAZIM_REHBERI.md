# Quami — Modül Yazım Rehberi

Bu belge yeni bir iş modülünün nasıl yazılacağını anlatır. Menü sırasıyla 22
modül yazılacak; hepsi buradaki adımlarla ve kurallarla çıkarsa ürün tek elden
çıkmış gibi durur, bakımı da kolay olur.

İskelet bitmiştir. Modül yazarken iskelete dokunmak gerekmez; gerekiyorsa
sebebini önce konuşun.

Kısaltmalar ilk geçtikleri yerde açık yazılmıştır.

---

## 1. Bir modül eklemek: sıra

Her modül aynı sekiz adımla çıkar. Adımları atlamayın; sıraları birbirine bağlı.

### 1.1. Domain sınıfları

`src/Quami.Domain/Entities/` altına modülün varlık sınıflarını yazın.

- Kiracıya ait her varlık `TenantEntity` sınıfından türer. Bu sınıf kimlik,
  `TenantId`, eski üründeki kayda karşılık gelen `LegacyId`, oluşturma ve
  güncelleme bilgileri ile mantıksal silme alanlarını getirir.
- Kiracıya ait olmayan sistem geneli bir tanım yazıyorsanız (çok nadir)
  `BaseEntity` kullanın.
- Enum değerlerini `src/Quami.Domain/Enums/` altına koyun.
- Domain katmanının hiçbir dış bağımlılığı yoktur. Burada Entity Framework Core
  (nesne-ilişki eşleyici), ASP.NET veya başka bir kütüphane kullanılmaz.

```csharp
public class Document : TenantEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DocumentStatus Status { get; set; }
}
```

### 1.2. Veritabanı yapılandırması

`src/Quami.Infrastructure/Persistence/Configurations/` altına
`<Varlık>Configuration` sınıfı yazın ve `BaseEntityConfiguration<T>` sınıfından
türetin.

- Alan uzunluklarını ve zorunlulukları burada verin.
- Benzersiz indeks gerekiyorsa **mutlaka** `HasActiveUniqueIndex` yardımcısını
  kullanın. Bu, indeksi yalnızca silinmemiş satırları kapsayacak şekilde kurar;
  yoksa mantıksal olarak silinmiş bir kayıt, aynı kodla yeni kayıt açmayı
  engeller.
- Enum alanları veritabanında metin olarak tutulur: `HasConversion<string>()`.
- Yabancı anahtarlarda `DeleteBehavior.Restrict` kullanın; fiziksel silme yok.

Yeni varlık için `QuamiDbContext` içine bir `DbSet` ekleyin.

### 1.3. Migration

```
dotnet ef migrations add <ModulAdi> --project src/Quami.Infrastructure --output-dir Persistence/Migrations
dotnet ef database update --project src/Quami.Infrastructure
```

Ürettiğiniz migration dosyasını açıp okuyun. Beklemediğiniz bir tablo veya
sütun değişikliği varsa durun ve sebebini bulun.

### 1.4. Uygulama katmanı: arayüz ve model

`src/Quami.Application/<Modul>/` altına servis arayüzünü ve dışarı verilecek
modelleri yazın. Sayfa doğrudan veritabanına gitmez, bu arayüzü kullanır.

### 1.5. Altyapı katmanı: servis uygulaması

`src/Quami.Infrastructure/Services/` altına arayüzün uygulamasını yazın ve
`DependencyInjection.AddInfrastructure` içinde kaydedin.

**Okuma servisleri veritabanı bağlamını fabrikadan alır:**

```csharp
public sealed class DocumentService(IDbContextFactory<QuamiDbContext> dbFactory) : IDocumentService
{
    public async Task<IReadOnlyList<Document>> GetAllAsync(CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        return await db.Documents.AsNoTracking().ToListAsync(ct);
    }
}
```

Sebebi 4. bölümde.

### 1.6. Ekran

`src/Quami.Web/Components/Pages/` altına sayfayı yazın.

- Sayfa `Pages` klasöründe olduğu için oturum zorunluluğu kendiliğinden gelir.
  Ayrıca `[Authorize]` yazmanıza gerek yok.
- Adres, modülün veritabanındaki `Route` alanıyla **birebir aynı** olmalı.
  Yoksa menüden gidilen adres ile sayfanın adresi tutmaz.
- Sayfa `QuamiPageHeader` ile başlar, ortak bileşenlerle devam eder.
- Modül yazıldığında, o modülün adresini
  `Components/Pages/ModulePlaceholder.razor` dosyasının başındaki `@page`
  listesinden **çıkarın**. Liste bitince o dosyayı silin.

### 1.7. Ekran metinleri

Ekranda görünen her metin `src/Quami.Web/Resources/SharedResource.resx`
(Türkçe, varsayılan) ve `SharedResource.en.resx` (İngilizce) dosyalarına
eklenir. Anahtar adı `<Modul>.<Yer>` biçiminde olsun, örneğin
`Documents.Title`, `Documents.Empty`.

Sayfada kullanımı:

```razor
@inject IStringLocalizer<SharedResource> L
<h1>@L["Documents.Title"]</h1>
```

Modül adları bu dosyalarda **değildir**; onlar veritabanındaki `modules`
tablosunda Türkçe ve İngilizce olarak durur.

### 1.8. Menü kaydı

Menü koddan değil veriden beslenir. Modül zaten
`src/Quami.Infrastructure/Persistence/Seed/SeedData.cs` içinde tanımlı; adını,
sırasını, ikonunu veya adresini değiştirmeniz gerekirse orayı düzenleyin.
Uygulama açılışında bu değişiklik veritabanına yansır.

- Kimlikler `SeedIds.cs` içinde elle yazılmış sabit değerlerdir. **Var olan bir
  kimliği asla değiştirmeyin.** Yeni modül eklerken yeni numara verin.
- İkon adları [Lucide](https://lucide.dev) setindendir. Kullandığınız ikon
  `src/Quami.Web/wwwroot/lucide.svg` içinde yoksa önce oraya eklenmelidir.
- Modül ürün olarak hazır değilse `IsAvailable: false` verin; menüde soluk ve
  "yakında" olarak görünür, hiçbir kiracıya lisanslanmaz.

---

## 2. Menü gruplarının davranış kalıpları

Her menü grubu bir davranış kalıbı taşır. Grubun **ilk** modülü kalıbı kurar;
bu yavaş ilerler. Sonraki modüller o kalıptan türer ve hızlı çıkar. Bir gruba
modül eklerken kalıbı yeniden icat etmeyin, grubun ilk modülüne bakın.

| Grup | Kalıp | Ne demek |
|---|---|---|
| İşlerim | Olay / aksiyon zinciri | Bir olay açılır, sorumlusu ve süresi olur, adımlarla ilerler, kapanır. Görevler, Öneriler, Düzeltici faaliyet böyledir. |
| Süreç ve kayıt | Kayıt + onay akışı | Kayıt oluşturulur, sürüm ve onay aşamalarından geçer, yayımlanır, arşivlenir. Dokümanlar, Sözleşmeler, Kayıtlar böyledir. |
| Denetim ve değerlendirme | Program döngüsü | Envanter, soru listesi, program, gerçekleştirme ve rapor. Beş parça birlikte çalışır. Denetim yönetimi, Uyum, Performans, Tedarikçi değerleme, Eğitim, Anket, Arıza bakım-kalibrasyon böyledir. |
| Risk | Envanter + analiz + tedbir | Varlık veya süreç envanteri çıkarılır, risk analizi yapılır, tedbir tanımlanır ve izlenir. Tehdit modeli, Bilgi güvenliği, Kalite, İş sağlığı güvenliği böyledir. |

Sistem grubu (Raporlar, Kullanıcılar, Tanımlar) bir iş kalıbı taşımaz; yönetim
ekranlarıdır.

---

## 3. Uyulacak kurallar

Bunlar tartışmaya açık değil; ürünün tek elden çıkmış görünmesi ve bakımının
mümkün kalması bunlara bağlı.

1. **Kiracıya ait her varlık `TenantEntity` sınıfından türer.** Kiracı süzgeci
   ve mantıksal silme böyle çalışır.
2. **Elle kiracı süzgeci yazmayın.** `Where(x => x.TenantId == ...)` yazmanıza
   gerek yok; süzgeç veritabanı bağlamında geneldir ve kendiliğinden uygulanır.
3. **Ortak bileşenleri kullanın.** Tablo, form alanı, buton, uyarı kutusu,
   pencere, sayfa başlığı, yükleniyor göstergesi hazır. Listesi ve canlı örneği
   `/dev/components` adresindedir.
4. **Sayfaya stil yazmayın.** Renk, kenarlık, boşluk gibi şeyler
   `src/Quami.Web/wwwroot/app.css` içindeki değişkenlerden ve ortak sınıflardan
   gelir. Yeni bir görsel parça gerekiyorsa ortak bileşen olarak ekleyin.
5. **Ekran metnini koda yazmayın.** Metin kaynak dosyalarından gelir.
6. **Okuma servislerinde bağlam fabrikasını kullanın**, paylaşılan veritabanı
   bağlamını değil.
7. **Dosya işlemlerinde `IFileStorage` kullanın.** Diske doğrudan yazmayın.
   Kiracı klasörü ve dosya adı üretimi orada halledilir. Dosyanın gerçek adını
   kendi tablonuzda saklayın.
8. **Silme mantıksaldır.** `Remove` çağırın; altyapı bunu işaretlemeye çevirir.
   Kayıt fiziksel olarak silinmez, denetim izi kaybolmaz.
9. **Bir modül yazdıktan sonra test edin, sonra diğerine geçin.** Yarım modül
   bırakıp sıradakine geçmeyin.

---

## 4. Sık yapılan hatalar

### 4.1. Paylaşılan veritabanı bağlamını iki yerde birden kullanmak

**Belirti:** "A second operation was started on this context instance before a
previous operation completed" hatası ve ekranın altında kırmızı hata çubuğu.

**Sebep:** Blazor Server'da bir kullanıcı oturumu (devre) içinde birden çok
bileşen aynı anda sorgu çalıştırabilir. Tek bir paylaşılan bağlam bunu
kaldırmaz.

**Doğrusu:** Okuma servisi `IDbContextFactory<QuamiDbContext>` alır ve her
çağrıda kendi kısa ömürlü bağlamını açar:

```csharp
await using var db = await dbFactory.CreateDbContextAsync(ct);
```

Tek iş parçacıklı akışlar (giriş, açılıştaki seed, dil ucu) doğrudan bağlam
kullanabilir; bileşenlerden çağrılan her şey fabrikayı kullanmalıdır.

### 4.2. Metni koda gömmek

**Yanlış:** `<h1>Dokümanlar</h1>`
**Doğru:** `<h1>@L["Documents.Title"]</h1>` ve iki kaynak dosyasına da anahtarı
eklemek.

Unutulan tek bir metin, İngilizce ekranda Türkçe olarak karşınıza çıkar.

### 4.3. Rengi koda gömmek

**Yanlış:** `<div style="color:#1E3556">`
**Doğru:** `<div class="quami-muted">` ya da gerekiyorsa `app.css` içinde
değişken kullanan yeni bir ortak sınıf.

Tema bir gün değişirse tek dosya değişecek; sayfalara dağılmış renkler bunu
imkânsız kılar.

### 4.4. Elle kiracı süzgeci yazmak

**Yanlış:** `db.Documents.Where(d => d.TenantId == tenantId)`
**Doğru:** `db.Documents` — süzgeç kendiliğinden uygulanır.

Elle yazmak iki soruna yol açar: bir yerde unutulursa veri sızar, ayrıca kiracı
ayrım yöntemi ileride değişirse (ayrı şema veya ayrı veritabanı) her sorguyu tek
tek düzeltmek gerekir.

Süzgeci bilerek devre dışı bırakmanız gereken nadir durumlarda (giriş anında
kullanıcıyı bulmak gibi) adlandırılmış süzgeci kapatın ve kiracıyı açıkça
yazın:

```csharp
db.BusinessUsers.IgnoreQueryFilters([QuamiDbContext.TenantFilter])
```

### 4.5. Kendi kullanıcı tablomuzu Identity'nin tablosuyla karıştırmak

İş tarafındaki kullanıcılar `db.BusinessUsers` üzerindedir. `db.Users` ASP.NET
Core Identity'nin kendi kullanıcı kümesidir ve kimlik doğrulama kaydını tutar.
İkisi aynı kimliği paylaşır ama farklı şeylerdir.

### 4.6. Adres uyuşmazlığı

Sayfanın `@page` adresi ile modülün veritabanındaki `Route` alanı aynı değilse
menü bağlantısı sayfayı bulamaz ve lisans kontrolü beklenmedik davranır.

---

## 5. Hazır olan altyapı

Modül yazarken bunları yeniden yazmanız gerekmez:

| Konu | Nerede |
|---|---|
| Çok kiracılılık ve süzgeçler | `QuamiDbContext` |
| Mantıksal silme | `BaseEntity`, `QuamiDbContext` |
| Denetim günlüğü (veri değişiklikleri) | `QuamiDbContext`, `audit_logs` tablosu |
| Kimlik olay günlüğü (giriş, çıkış, kilit) | `auth_events` tablosu |
| Kimlik ve oturum | `IAuthenticationProvider`, `ITenantContext` |
| Modül lisansı | `IModuleLicenseService`, `IMenuService` |
| Menü | `IMenuService`, `menu_groups` ve `modules` tabloları |
| Dosya deposu | `IFileStorage` |
| Ortak bileşenler | `Components/Shared/`, örnekler `/dev/components` |
| Tema | `wwwroot/app.css` |
| Çok dillilik | `Resources/SharedResource*.resx` |
