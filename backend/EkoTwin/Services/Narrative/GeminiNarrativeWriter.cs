using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace EkoTwin.Services.Narrative;

/// <summary>
/// Gemini ayarları (appsettings "Gemini" bölümü). API anahtarı user-secrets'ta tutulur, asla repoya yazılmaz:
///   dotnet user-secrets set "Gemini:ApiKey" "..."
///   dotnet user-secrets set "Gemini:Model" "..."   (model adı Google'ın güncel model listesinden seçilir)
/// </summary>
public class GeminiOptions
{
    public string? ApiKey { get; set; }
    public string? Model { get; set; }
    public string Endpoint { get; set; } = "https://generativelanguage.googleapis.com/v1beta";
    public int TimeoutSeconds { get; set; } = 30;
    public double Temperature { get; set; } = 0.3;
    public int MaxOutputTokens { get; set; } = 2048;

    public bool IsConfigured => !string.IsNullOrWhiteSpace(ApiKey) && !string.IsNullOrWhiteSpace(Model);
}

/// <summary>
/// Gemini API (generateContent) ile metin üretir.
/// POST {Endpoint}/models/{Model}:generateContent, anahtar "x-goog-api-key" başlığında.
/// Kaynak: https://ai.google.dev/api/generate-content
/// </summary>
public class GeminiNarrativeWriter : INarrativeWriter
{
    private readonly HttpClient _http;
    private readonly GeminiOptions _options;

    public GeminiNarrativeWriter(HttpClient http, IOptions<GeminiOptions> options)
    {
        _http = http;
        _options = options.Value;
        _http.Timeout = TimeSpan.FromSeconds(Math.Max(5, _options.TimeoutSeconds));
    }

    public bool IsConfigured => _options.IsConfigured;
    public string ProviderName => "gemini";
    public string? ModelName => _options.Model;

    public async Task<string> WriteAsync(string systemPrompt, string userPrompt, CancellationToken ct = default)
    {
        if (!IsConfigured)
            throw new NarrativeProviderException("Gemini ayarlı değil (API anahtarı veya model eksik).");

        var url = $"{_options.Endpoint.TrimEnd('/')}/models/{Uri.EscapeDataString(_options.Model!)}:generateContent";
        var body = new GeminiRequest(
            new GeminiContent(null, new[] { new GeminiPart(systemPrompt) }),
            new[] { new GeminiContent("user", new[] { new GeminiPart(userPrompt) }) },
            new GeminiGenerationConfig(_options.Temperature, _options.MaxOutputTokens));

        using var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = JsonContent.Create(body, options: Json) };
        request.Headers.Add("x-goog-api-key", _options.ApiKey);

        HttpResponseMessage response;
        try
        {
            response = await _http.SendAsync(request, ct);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException && !ct.IsCancellationRequested)
        {
            throw new NarrativeProviderException("Gemini'ye bağlanılamadı veya yanıt süresi aşıldı.", ex);
        }

        using (response)
        {
            if (!response.IsSuccessStatusCode)
            {
                // Hata gövdesi loglanabilir ama anahtar içermez; kullanıcıya yalnızca durum kodu yansıtılır.
                throw new NarrativeProviderException($"Gemini hata döndürdü (HTTP {(int)response.StatusCode}).");
            }

            var json = await response.Content.ReadFromJsonAsync<JsonElement>(Json, ct);
            var text = ExtractText(json);
            if (string.IsNullOrWhiteSpace(text))
                throw new NarrativeProviderException("Gemini boş yanıt döndürdü.");
            return text;
        }
    }

    /// <summary>candidates[0].content.parts[*].text (düşünce parçaları hariç) birleştirilir.</summary>
    internal static string ExtractText(JsonElement json)
    {
        if (!json.TryGetProperty("candidates", out var candidates) || candidates.GetArrayLength() == 0)
            return string.Empty;

        var first = candidates[0];
        if (!first.TryGetProperty("content", out var content) || !content.TryGetProperty("parts", out var parts))
            return string.Empty;

        var texts = parts.EnumerateArray()
            .Where(p => !(p.TryGetProperty("thought", out var t) && t.ValueKind == JsonValueKind.True))
            .Where(p => p.TryGetProperty("text", out _))
            .Select(p => p.GetProperty("text").GetString());

        return string.Concat(texts).Trim();
    }

    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private record GeminiPart(string Text);
    private record GeminiContent(string? Role, GeminiPart[] Parts);
    private record GeminiGenerationConfig(double Temperature, int MaxOutputTokens);
    private record GeminiRequest(GeminiContent SystemInstruction, GeminiContent[] Contents, GeminiGenerationConfig GenerationConfig);
}
