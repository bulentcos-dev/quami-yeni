namespace Quami.Api.Authentication;

/// <summary>
/// API anahtarı ayarları. Anahtarlar ayar dosyasından okunur, koda gömülmez.
///
/// Örnek:
/// "Api": { "Keys": { "python-ai": "...", "jira": "..." } }
///
/// Anahtar tanımlı değilse korumalı uçlar 401 döner. Varsayılan anahtar
/// BİLEREK yoktur: kimse farkında olmadan açık bir API ile çalışmasın.
/// </summary>
public sealed class ApiKeyOptions
{
    public const string SectionName = "Api";

    /// <summary>İstemci adı → anahtar. Ad yalnızca günlükte kimin çağırdığını göstermek içindir.</summary>
    public Dictionary<string, string> Keys { get; set; } = [];
}
