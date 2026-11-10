using EkoTwin.Services.Narrative;

namespace EkoTwin.Tests.Helpers;

/// <summary>
/// Testler için sahte yapay zekâ yazıcısı: gerçek servis çağrılmaz, ücret ve internet gerekmez.
/// Dönecek metin, fırlatılacak hata ve gönderilen talimatlar testten kontrol edilir.
/// </summary>
public class FakeNarrativeWriter : INarrativeWriter
{
    public bool IsConfigured { get; set; } = true;
    public string ProviderName => "fake";
    public string? ModelName { get; set; } = "fake-model";

    /// <summary>Bilgi kartından yanıt üreten fonksiyon (varsayılan: sabit metin).</summary>
    public Func<string, string>? Respond { get; set; }

    public Exception? Throw { get; set; }

    public string? LastSystemPrompt { get; private set; }
    public string? LastUserPrompt { get; private set; }
    public int Calls { get; private set; }

    public Task<string> WriteAsync(string systemPrompt, string userPrompt, CancellationToken ct = default)
    {
        Calls++;
        LastSystemPrompt = systemPrompt;
        LastUserPrompt = userPrompt;

        if (Throw is not null)
            throw Throw;

        return Task.FromResult(Respond?.Invoke(userPrompt) ?? "");
    }
}
