namespace Quami.Application.Abstractions;

/// <summary>
/// Saklanan dosyanın kimliği ve bilgileri. <paramref name="StorageKey"/> deposa
/// yazılan ADIDIR; kullanıcının verdiği gerçek ad <paramref name="FileName"/>
/// alanındadır ve çağıran modül tarafından VERİTABANINDA saklanır.
/// </summary>
public sealed record StoredFile(string StorageKey, string FileName, string ContentType, long Length);

/// <summary>
/// Dosya deposu.
///
/// Kiracı ayrımı burada zorlanır: arayüzde kiracı parametresi YOKTUR, uygulama
/// kiracıyı <see cref="ITenantContext"/>'ten okur. Böylece bir modül yanlışlıkla
/// başka bir kiracının dosyasına erişemez; kontrol sunucudadır.
///
/// Diskteki ad kullanıcının verdiği ad DEĞİLDİR, üretilen bir kimliktir.
/// Türkçe karakter, boşluk ve aynı adlı dosya sorunları böylece oluşmaz.
///
/// Bugünkü uygulama diskte tutar (LocalFileStorage). S3'e geçiş yalnızca bu
/// arayüzün ikinci bir uygulamasıdır; çağıran kod değişmez.
/// </summary>
public interface IFileStorage
{
    /// <summary>Dosyayı kaydeder ve depo kimliğini döndürür.</summary>
    Task<StoredFile> SaveAsync(
        string fileName, string? contentType, Stream content, CancellationToken cancellationToken = default);

    /// <summary>Dosyayı okur. Kayıt yoksa null döner.</summary>
    Task<Stream?> GetAsync(string storageKey, CancellationToken cancellationToken = default);

    /// <summary>Dosyayı siler. Kayıt yoksa sessizce geçer.</summary>
    Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Dosyanın adresi. Diskte tutulurken uygulamanın indirme ucunu verir;
    /// S3'e geçilince imzalı adres döndürecek.
    /// </summary>
    string GetUrl(string storageKey);
}
