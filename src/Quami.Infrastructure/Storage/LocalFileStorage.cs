using System.Text.RegularExpressions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Quami.Application.Abstractions;

namespace Quami.Infrastructure.Storage;

/// <summary>
/// Dosyaları diskte tutan uygulama.
///
/// Yerleşim: &lt;kök&gt;/&lt;kiracı kimliği&gt;/&lt;üretilen ad&gt;
/// Kiracı klasörü <see cref="ITenantContext"/>'ten gelir, çağırandan DEĞİL.
/// Her okuma ve silmede iki kontrol yapılır: depo adı beklenen biçimde mi ve
/// çözülen tam yol kiracının klasörünün İÇİNDE mi (dizin aşımına karşı).
/// </summary>
public sealed partial class LocalFileStorage(
    ITenantContext tenantContext,
    IOptions<FileStorageOptions> options,
    IHostEnvironment environment) : IFileStorage
{
    private readonly FileStorageOptions _options = options.Value;

    public async Task<StoredFile> SaveAsync(
        string fileName, string? contentType, Stream content, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        ArgumentNullException.ThrowIfNull(content);

        var tenantRoot = EnsureTenantRoot();

        // Diskteki ad üretilir; kullanıcının verdiği ad yalnızca uzantı için
        // kullanılır ve gerçek ad çağıran tarafından veritabanında saklanır.
        var storageKey = Guid.CreateVersion7().ToString("N") + SafeExtension(fileName);
        var fullPath = Path.Combine(tenantRoot, storageKey);

        await using (var target = File.Create(fullPath))
        {
            await content.CopyToAsync(target, cancellationToken);
        }

        var length = new FileInfo(fullPath).Length;

        return new StoredFile(
            storageKey,
            fileName,
            string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType,
            length);
    }

    public Task<Stream?> GetAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        var fullPath = ResolveExisting(storageKey);
        Stream? stream = fullPath is null ? null : File.OpenRead(fullPath);
        return Task.FromResult(stream);
    }

    public Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        var fullPath = ResolveExisting(storageKey);
        if (fullPath is not null)
            File.Delete(fullPath);

        return Task.CompletedTask;
    }

    public string GetUrl(string storageKey)
    {
        EnsureValidKey(storageKey);
        return $"{_options.DownloadPathPrefix.TrimEnd('/')}/{storageKey}";
    }

    // ---- iç işler ----

    private string TenantRoot()
    {
        var tenantId = tenantContext.TenantId
            ?? throw new InvalidOperationException("Dosya işlemi için kiracı bağlamı yok.");

        var root = Path.IsPathRooted(_options.RootPath)
            ? _options.RootPath
            : Path.Combine(environment.ContentRootPath, _options.RootPath);

        return Path.GetFullPath(Path.Combine(root, tenantId.ToString("N")));
    }

    private string EnsureTenantRoot()
    {
        var tenantRoot = TenantRoot();
        Directory.CreateDirectory(tenantRoot);
        return tenantRoot;
    }

    /// <summary>Geçerliyse ve dosya varsa tam yolu, yoksa null döner.</summary>
    private string? ResolveExisting(string storageKey)
    {
        EnsureValidKey(storageKey);

        var tenantRoot = TenantRoot();
        var fullPath = Path.GetFullPath(Path.Combine(tenantRoot, storageKey));

        // Dizin aşımı koruması: çözülen yol kiracının klasörünün dışına çıkamaz.
        if (!fullPath.StartsWith(tenantRoot + Path.DirectorySeparatorChar, StringComparison.Ordinal))
            throw new UnauthorizedAccessException("Dosya yolu kiracının klasörünün dışında.");

        return File.Exists(fullPath) ? fullPath : null;
    }

    private static void EnsureValidKey(string storageKey)
    {
        if (string.IsNullOrWhiteSpace(storageKey) || !StorageKeyPattern().IsMatch(storageKey))
            throw new ArgumentException("Geçersiz depo adı.", nameof(storageKey));
    }

    /// <summary>Uzantıyı güvenli hale getirir: en fazla 10 karakter, yalnızca harf ve rakam.</summary>
    private static string SafeExtension(string fileName)
    {
        var extension = Path.GetExtension(fileName);
        if (string.IsNullOrWhiteSpace(extension)) return string.Empty;

        extension = extension.TrimStart('.');
        extension = new string(extension.Where(char.IsLetterOrDigit).ToArray());

        if (extension.Length == 0) return string.Empty;
        if (extension.Length > 10) extension = extension[..10];

        return "." + extension.ToLowerInvariant();
    }

    [GeneratedRegex(@"^[0-9a-f]{32}(\.[a-z0-9]{1,10})?$", RegexOptions.CultureInvariant)]
    private static partial Regex StorageKeyPattern();
}
