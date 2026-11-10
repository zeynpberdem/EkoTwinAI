using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EkoTwin.Data;
using EkoTwin.Models;
using EkoTwin.Services;
using EkoTwin.Services.Narrative;
using EkoTwin.Tests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging.Abstractions;

namespace EkoTwin.Tests;

/// <summary>
/// A7: Yapay zekâ rapor mektubu. Gerçek yapay zekâ çağrılmaz; sahte yazıcı kullanılır.
/// Asıl test edilen: otomatik kontroller, şablona düşme, gizlilik ve saklama.
/// </summary>
public class NarrativeTests
{
    /// <summary>Sayı, jargon ve vaat içermeyen, kısa cümleli, ~110 kelimelik geçerli bir mektup gövdesi.</summary>
    public const string GoodText =
        "Firmanız enerji tasarrufu konusunda iyi bir yolda. Bu emeğiniz için sizi kutluyoruz. " +
        "Şimdi en acil işiniz kayıtlarınızı korumak. Düzenli yedek alın ve virüse karşı önlem kurun. " +
        "Bu iş az masraflı ve kısa sürede biter. Bilgisayar bozulsa da kayıtlarınız kaybolmaz.\n\n" +
        "İkinci adım olarak elektrik harcamasını gösteren sayaç taktırın. Böylece en çok elektrik yakan makineyi görürsünüz. " +
        "Tasarrufa oradan başlarsınız. Üçüncü adım olarak firmanızın yıllık karbonunu hesaplayın. " +
        "Müşteri sorduğunda cevabınız hazır olur.\n\n" +
        "Bu adımları sırayla atarsanız işiniz daha düzenli olur. Masraflarınız da azalır. " +
        "Testi bir süre sonra yeniden doldurun. Böylece ne kadar ilerlediğinizi görürsünüz. Size kolaylıklar dileriz.";

    // ===================== Otomatik kontrol =====================

    [Fact]
    public void GecerliMetin_KontroldenGecer()
    {
        var result = NarrativeValidator.Validate(GoodText, new[] { "Genel puan 100 üzerinden 44." });

        Assert.True(result.IsValid, string.Join("\n", result.Errors));
    }

    [Fact]
    public void BilgiKartindaOlmayanSayi_Reddedilir()
    {
        var result = NarrativeValidator.Validate(GoodText + " Bu yıl 4.321 lira kazanırsınız.", new[] { "Genel puan 100 üzerinden 44." });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("olmayan sayı") && e.Contains("4321"));
    }

    [Fact]
    public void BilgiKartindakiSayilar_KabulEdilir()
    {
        var facts = new[] { "Genel puan 100 üzerinden 44.", "Firmanızın 2025 yılı karbonu: yaklaşık 1.210 ton", "Karbonunuz en çok elektrikten geliyor (yüzde 66)." };
        var text = GoodText + " Genel puanınız 100 üzerinden 44. Karbonunuz yaklaşık 1.210 ton, yüzde 66'sı elektrikten.";

        var result = NarrativeValidator.Validate(text, facts);

        Assert.DoesNotContain(result.Errors, e => e.Contains("olmayan sayı"));
    }

    [Theory]
    [InlineData(" Bunun için ERP kurun.", "teknik terim")]
    [InlineData(" Emisyon değeriniz düşecek.", "teknik terim")]
    [InlineData(" Bu adımlarla kesinlikle kazanırsınız.", "vaat")]
    [InlineData(" Size garanti veriyoruz.", "vaat")]
    public void JargonVeVaat_Reddedilir(string addition, string expectedError)
    {
        var result = NarrativeValidator.Validate(GoodText + addition, Array.Empty<string>());

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains(expectedError));
    }

    [Fact]
    public void CokKisaMetin_Reddedilir()
    {
        var result = NarrativeValidator.Validate("Firmanız iyi durumda. Tebrikler.", Array.Empty<string>());

        Assert.Contains(result.Errors, e => e.Contains("uzunluğu"));
    }

    [Theory]
    [InlineData("1.210 ton", 1210)]
    [InlineData("4,8 ton", 4.8)]
    [InlineData("yüzde 66'sı", 66)]
    [InlineData("2027 yılında.", 2027)]
    public void Sayilar_TurkceYazimlaOkunur(string text, double expected) =>
        Assert.Equal((decimal)expected, NarrativeValidator.Numbers(text).Single());

    [Fact]
    public void Temizleme_BicimlendirmeIsaretleriniKaldirir()
    {
        var cleaned = NarrativeValidator.Clean("## Başlık\n**Önemli:** metin\n- madde");

        Assert.Equal("Başlık\nÖnemli: metin\nmadde", cleaned);
    }

    [Fact]
    public void Talimat_JargonListesiniVeKurallariIcerir()
    {
        Assert.Contains("Bilgi kartında olmayan hiçbir sayıyı yazma", NarrativePrompt.System);
        Assert.Contains("CBAM", NarrativePrompt.System);
        Assert.StartsWith("BİLGİ KARTI:\n- a", NarrativePrompt.User(new[] { "a", "b" }));
    }

    // ===================== Gemini yanıtını okuma =====================

    [Fact]
    public void GeminiYaniti_MetinParcalariBirlesir_DusunceParcalariAtlanir()
    {
        var json = JsonDocument.Parse("""
            {"candidates":[{"content":{"role":"model","parts":[
              {"text":"iç düşünce","thought":true},
              {"text":"Merhaba "},
              {"text":"dünya."}
            ]}}]}
            """).RootElement;

        Assert.Equal("Merhaba dünya.", GeminiNarrativeWriter.ExtractText(json));
    }

    [Fact]
    public void GeminiYaniti_AdayYoksaBos()
    {
        var json = JsonDocument.Parse("""{"candidates":[]}""").RootElement;

        Assert.Equal("", GeminiNarrativeWriter.ExtractText(json));
    }

    [Fact]
    public void GeminiAyarlari_AnahtarVeModelOlmadanAyarliSayilmaz()
    {
        Assert.False(new GeminiOptions().IsConfigured);
        Assert.False(new GeminiOptions { ApiKey = "x" }.IsConfigured);
        Assert.True(new GeminiOptions { ApiKey = "x", Model = "m" }.IsConfigured);
    }

    // ===================== Servis =====================

    private static NarrativeService Service(AppDbContext db, INarrativeWriter writer)
    {
        var clock = FixedTimeProvider.At(2026, 8, 1);
        return new NarrativeService(db, new AssessmentService(db), new CarbonFootprintService(db, clock),
            new ProgressService(db, clock), new BenchmarkService(db), writer, clock, NullLogger<NarrativeService>.Instance);
    }

    private static async Task<(AppDbContext db, Company company, Guid assessmentId)> ScenarioAsync()
    {
        var db = TestDb.Create();
        var company = await TestDb.AddCompanyAsync(db, "Gizli Döküm Ltd.");
        var id = await TestDb.AddAssessmentAsync(db, company.Id, n => n <= 8 ? 2 : 3, new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc));
        return (db, company, id);
    }

    [Fact]
    public async Task YapayZekaAyarliDegil_KuralTabanliMetin()
    {
        var (db, company, id) = await ScenarioAsync();
        await using var _db = db;
        var writer = new FakeNarrativeWriter { IsConfigured = false };

        var n = (await Service(db, writer).GenerateAsync(id))!;

        Assert.Equal(NarrativeSource.Template, n.Source);
        Assert.Equal("template", n.Provider);
        Assert.Contains("ayarlı değil", n.FallbackReason);
        Assert.StartsWith($"Sayın {company.CompanyName} yetkilisi,", n.Text);
        Assert.Contains("en acil adımlar", n.Text);
        Assert.Contains(NarrativeTemplate.Closing, n.Text);
        Assert.Equal(0, writer.Calls);
    }

    [Fact]
    public async Task GecerliYapayZekaMetni_Kullanilir()
    {
        var (db, company, id) = await ScenarioAsync();
        await using var _db = db;
        var writer = new FakeNarrativeWriter { Respond = _ => "**" + GoodText + "**" };

        var n = (await Service(db, writer).GenerateAsync(id))!;

        Assert.Equal(NarrativeSource.Ai, n.Source);
        Assert.Equal("fake", n.Provider);
        Assert.Equal("fake-model", n.Model);
        Assert.Equal(NarrativePrompt.Version, n.PromptVersion);
        Assert.Null(n.FallbackReason);
        Assert.Equal($"Sayın {company.CompanyName} yetkilisi,\n\n{GoodText}", n.Text); // biçim işaretleri temizlendi
    }

    [Fact]
    public async Task UydurmaSayiliMetin_SablonaDuser_NedenKaydedilir()
    {
        var (db, _, id) = await ScenarioAsync();
        await using var _db = db;
        var writer = new FakeNarrativeWriter { Respond = _ => GoodText + " Bu yıl 4.321 lira kazanırsınız." };

        var n = (await Service(db, writer).GenerateAsync(id))!;

        Assert.Equal(NarrativeSource.Template, n.Source);
        Assert.Contains("Kontrolden geçmedi", n.FallbackReason);
        Assert.DoesNotContain("4.321", n.Text);
    }

    [Fact]
    public async Task YapayZekaHataVerirse_SablonaDuser()
    {
        var (db, _, id) = await ScenarioAsync();
        await using var _db = db;
        var writer = new FakeNarrativeWriter { Throw = new NarrativeProviderException("Gemini hata döndürdü (HTTP 429).") };

        var n = (await Service(db, writer).GenerateAsync(id))!;

        Assert.Equal(NarrativeSource.Template, n.Source);
        Assert.Equal("Gemini hata döndürdü (HTTP 429).", n.FallbackReason);
    }

    [Fact]
    public async Task Gizlilik_FirmaAdiYapayZekayaGonderilmez()
    {
        var (db, company, id) = await ScenarioAsync();
        await using var _db = db;
        var writer = new FakeNarrativeWriter { Respond = _ => GoodText };

        await Service(db, writer).GenerateAsync(id);

        Assert.NotNull(writer.LastUserPrompt);
        Assert.DoesNotContain(company.CompanyName, writer.LastUserPrompt);
        Assert.DoesNotContain(company.CompanyName, writer.LastSystemPrompt);
        Assert.Contains("En acil adım 1:", writer.LastUserPrompt); // bilgi kartı kuralların sonucunu taşır
    }

    [Fact]
    public async Task BilgiKarti_SadeVeJargonsuz()
    {
        var (db, _, id) = await ScenarioAsync();
        await using var _db = db;
        var writer = new FakeNarrativeWriter { Respond = _ => GoodText };

        await Service(db, writer).GenerateAsync(id);

        var problems = NarrativeValidator.Jargon
            .Where(t => System.Globalization.CultureInfo.GetCultureInfo("tr-TR").CompareInfo
                .IndexOf(writer.LastUserPrompt!, t, System.Globalization.CompareOptions.IgnoreCase) >= 0)
            .ToList();
        Assert.Empty(problems);
    }

    [Fact]
    public async Task HerUretimSaklanir_EnSonuDoner_DegerlendirmeYoksaNull()
    {
        var (db, _, id) = await ScenarioAsync();
        await using var _db = db;
        var service = Service(db, new FakeNarrativeWriter { IsConfigured = false });

        var first = await service.GenerateAsync(id);
        // Testte saat sabit; ilk mektubu bir saat öncesine çekerek "en son" ayrımını netleştir.
        db.AssessmentNarratives.Single(n => n.Id == first!.Id).CreatedAt = first!.CreatedAt.AddHours(-1);
        await db.SaveChangesAsync();
        var second = await service.GenerateAsync(id);

        Assert.Equal(2, db.AssessmentNarratives.Count());
        Assert.Equal(second!.Id, (await service.GetLatestAsync(id))!.Id);
        Assert.Null(await service.GenerateAsync(Guid.NewGuid()));
        Assert.Null(await service.GetLatestAsync(Guid.NewGuid()));
    }

    // ===================== Uç nokta =====================

    [Fact]
    public async Task Api_UretOkuVeYoksa404()
    {
        var fake = new FakeNarrativeWriter { Respond = _ => GoodText };
        using var factory = new ApiFactory
        {
            ConfigureTestServices = s =>
            {
                s.RemoveAll<INarrativeWriter>();
                s.AddSingleton<INarrativeWriter>(fake);
            }
        };
        var client = factory.CreateClientWithDatabase();

        var company = await (await client.PostAsJsonAsync("/api/companies", new
        {
            companyName = "Mektup Gıda", sector = "FoodAndBeverage", employeeCount = 20, exportsToEu = false, cbamProductGroup = "None"
        })).Content.ReadFromJsonAsync<JsonElement>();
        var questions = await client.GetFromJsonAsync<JsonElement>("/api/questions");
        var assessment = await (await client.PostAsJsonAsync("/api/assessments", new
        {
            companyId = company.GetProperty("id").GetString(),
            answers = questions.EnumerateArray().Select(q => new { questionId = q.GetProperty("id").GetString(), score = 2 })
        })).Content.ReadFromJsonAsync<JsonElement>();
        var url = $"/api/assessments/{assessment.GetProperty("id").GetString()}/narrative";

        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync(url)).StatusCode); // henüz üretilmedi

        var post = await client.PostAsync(url, null);
        var json = await post.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(HttpStatusCode.OK, post.StatusCode);
        Assert.Equal("Ai", json.GetProperty("source").GetString());
        Assert.StartsWith("Sayın Mektup Gıda yetkilisi,", json.GetProperty("text").GetString());

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(url)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,
            (await client.PostAsync($"/api/assessments/{Guid.NewGuid()}/narrative", null)).StatusCode);
    }
}
