namespace EkoTwin.Services.Narrative;

/// <summary>
/// Yapay zekâ metin yazıcısı. Sağlayıcıdan bağımsızdır: bugün Gemini, ileride başka bir model
/// (Claude, yerel model) yalnızca bu arayüzü uygulayan yeni bir sınıfla eklenebilir.
/// Testlerde sahte (fake) bir yazıcı kullanılır; gerçek servis çağrılmaz.
/// </summary>
public interface INarrativeWriter
{
    /// <summary>Anahtar ve model tanımlı mı? Değilse kural tabanlı şablon kullanılır.</summary>
    bool IsConfigured { get; }

    /// <summary>Sağlayıcı adı (örn. "gemini").</summary>
    string ProviderName { get; }

    /// <summary>Kullanılan model adı.</summary>
    string? ModelName { get; }

    /// <summary>Talimat (system) ve bilgi kartı (user) ile metin üretir. Hata durumunda istisna fırlatır.</summary>
    Task<string> WriteAsync(string systemPrompt, string userPrompt, CancellationToken ct = default);
}

/// <summary>Yapay zekâ servisinden kaynaklanan hata (bağlantı, kota, boş yanıt vb.).</summary>
public class NarrativeProviderException : Exception
{
    public NarrativeProviderException(string message, Exception? inner = null) : base(message, inner)
    {
    }
}
