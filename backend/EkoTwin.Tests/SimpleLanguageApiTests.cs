using System.Net.Http.Json;
using System.Text.Json;
using EkoTwin.Tests.Helpers;

namespace EkoTwin.Tests;

/// <summary>Sade dil alanlarının API'den doğru döndüğü (soru kartları ve rapor).</summary>
public class SimpleLanguageApiTests
{
    [Fact]
    public async Task Sorular_SadeMetinAciklamaVeBesSecenekDoner()
    {
        using var factory = new ApiFactory();
        var client = factory.CreateClientWithDatabase();

        var questions = await client.GetFromJsonAsync<JsonElement>("/api/questions");
        var q1 = questions[0];

        Assert.Equal(16, questions.GetArrayLength());
        Assert.Equal("Muhasebe, stok ve satışı bir bilgisayar programında mı takip ediyorsunuz?", q1.GetProperty("simpleText").GetString());
        Assert.Contains("ERP", q1.GetProperty("helpText").GetString());
        Assert.Equal("Dijital düzen", q1.GetProperty("dimensionSimpleName").GetString());

        var options = q1.GetProperty("options");
        Assert.Equal(5, options.GetArrayLength());
        Assert.Equal(1, options[0].GetProperty("score").GetInt32());
        Assert.Equal("Hayır, defter veya kâğıtla tutuyoruz", options[0].GetProperty("label").GetString());

        // Açıklaması olmayan soru (4) için helpText null
        Assert.Equal(JsonValueKind.Null, questions[3].GetProperty("helpText").ValueKind);
    }

    [Fact]
    public async Task Rapor_SecilenCevabinMetniniVeSadeSeviyeyiGosterir()
    {
        using var factory = new ApiFactory();
        var client = factory.CreateClientWithDatabase();

        var company = await (await client.PostAsJsonAsync("/api/companies", new
        {
            companyName = "Sade Dil Gıda", sector = "FoodAndBeverage", employeeCount = 20,
            exportsToEu = false, cbamProductGroup = "None"
        })).Content.ReadFromJsonAsync<JsonElement>();

        var questions = await client.GetFromJsonAsync<JsonElement>("/api/questions");
        var answers = questions.EnumerateArray().Select(q => new { questionId = q.GetProperty("id").GetString(), score = 3 });
        var response = await client.PostAsJsonAsync("/api/assessments",
            new { companyId = company.GetProperty("id").GetString(), answers });
        var report = await response.Content.ReadFromJsonAsync<JsonElement>();

        // Tüm cevaplar 3 → skor 50 → "Gelişiyor", kademe 3
        Assert.Equal("Gelişen", report.GetProperty("maturityLevel").GetString());
        Assert.Equal("Gelişiyor", report.GetProperty("simpleMaturityLevel").GetString());
        Assert.Equal(3, report.GetProperty("maturityStage").GetInt32());

        var first = report.GetProperty("answers")[0];
        Assert.Equal("Bazı işler için program kullanıyoruz", first.GetProperty("answerLabel").GetString());
        Assert.Equal("Dijital düzen", report.GetProperty("dimensionScores")[0].GetProperty("simpleName").GetString());
    }
}
