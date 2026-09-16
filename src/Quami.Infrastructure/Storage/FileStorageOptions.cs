namespace Quami.Infrastructure.Storage;

/// <summary>
/// Dosya deposu ayarları. Ayar dosyasındaki <c>FileStorage</c> bölümünden okunur;
/// kök klasör koda gömülmez.
/// </summary>
public sealed class FileStorageOptions
{
    public const string SectionName = "FileStorage";

    /// <summary>
    /// Dosyaların kök klasörü. Göreli verilirse uygulamanın içerik köküne göre
    /// çözülür. Her kiracının dosyaları bu klasörün altında kendi alt
    /// klasöründe durur.
    /// </summary>
    public string RootPath { get; set; } = "App_Data/files";

    /// <summary>İndirme ucunun yol öneki. <see cref="Quami.Application.Abstractions.IFileStorage.GetUrl"/> bunu kullanır.</summary>
    public string DownloadPathPrefix { get; set; } = "/files";
}
