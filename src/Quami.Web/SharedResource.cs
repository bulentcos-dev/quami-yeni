namespace Quami.Web;

/// <summary>
/// Ortak metin kaynaklarının işaretçi sınıfı. Metinler
/// <c>Resources/SharedResource.resx</c> (Türkçe, varsayılan) ve
/// <c>Resources/SharedResource.en.resx</c> dosyalarındadır.
///
/// DİKKAT: bu sınıf projenin KÖK ad alanında durmalıdır. Ad alanı
/// <c>Quami.Web.Resources</c> olsaydı, ResourcesPath ile birleşince aranan yol
/// <c>Resources/Resources.SharedResource.resx</c> olur ve kaynaklar bulunamazdı.
///
/// Ekranda görünen hiçbir metin koda yazılmaz; hepsi buradan gelir.
/// </summary>
public sealed class SharedResource;
