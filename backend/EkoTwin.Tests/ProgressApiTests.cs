using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EkoTwin.Tests.Helpers;

namespace EkoTwin.Tests;

/// <summary>Gelişim takibi ve karbon hedefi uç noktalarını HTTP üzerinden uçtan uca test eder.</summary>
public class ProgressApiTests
{
    private static async Task<string> CreateCompanyAsync(HttpClient client, string name = "Gelişim Mobilya")
    {
        var response = await client.PostAsJsonAsync("/api/companies", new
        {
            companyName = name,
            sector = "FurnitureAndWood",
            employeeCount = 30,
            exportsToEu = false,
            cbamProductGroup = "None"
        });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetString()!;
    }

    private static async Task<string> CreateAssessmentAsync(HttpClient client, string companyId, Func<int, int> score)
    {
        var questions = await client.GetFromJsonAsync<JsonElement>("/api/questions");
        var answers = questions.EnumerateArray().Select(q => new
        {
            questionId = q.GetProperty("id").GetString(),
            score = score(q.GetProperty("displayOrder").GetInt32())
        });
        var response = await client.PostAsJsonAsync("/api/assessments", new { companyId, answers });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetString()!;
    }

    private static Task<HttpResponseMessage> AddFootprintAsync(HttpClient client, string companyId, int year, decimal kwh) =>
        client.PostAsJsonAsync($"/api/companies/{companyId}/carbon-footprints",
            new { year, entries = new[] { new { source = "Electricity", quantity = kwh } } });

    [Fact]
    public async Task Progress_IkiDegerlendirme_KarsilastirmaVeOneriTakibiDoner()
    {
        using var factory = new ApiFactory();
        var client = factory.CreateClientWithDatabase();
        var companyId = await CreateCompanyAsync(client);

        var first = await CreateAssessmentAsync(client, companyId, _ => 1);
        await Task.Delay(20); // farklı kayıt zamanı
        var second = await CreateAssessmentAsync(client, companyId, n => n <= 8 ? 4 : 1);

        var response = await client.GetAsync($"/api/companies/{companyId}/progress");
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(2, json.GetProperty("assessmentCount").GetInt32());
        var comparison = json.GetProperty("latestComparison");
        Assert.Equal(first, comparison.GetProperty("previous").GetProperty("id").GetString());
        Assert.Equal(second, comparison.GetProperty("current").GetProperty("id").GetString());

        var summary = comparison.GetProperty("recommendationFollowUp").GetProperty("summary");
        Assert.Equal(20, summary.GetProperty("previousCount").GetInt32()); // 16 aksiyon + 4 uyarı
        Assert.Equal(10, summary.GetProperty("improvedCount").GetInt32()); // S1-8 + Dijital + Enerji uyarısı

        Assert.Equal("Improved", comparison.GetProperty("recommendationFollowUp").GetProperty("items")
            .EnumerateArray().First(i => i.GetProperty("questionNumber").ValueKind == JsonValueKind.Number
                                         && i.GetProperty("questionNumber").GetInt32() == 1)
            .GetProperty("status").GetString());
        Assert.True(json.GetProperty("highlights").GetArrayLength() > 0);
    }

    [Fact]
    public async Task Progress_FirmaYoksa_404()
    {
        using var factory = new ApiFactory();
        var client = factory.CreateClientWithDatabase();

        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/companies/{Guid.NewGuid()}/progress")).StatusCode);
    }

    [Fact]
    public async Task Compare_200_400_404()
    {
        using var factory = new ApiFactory();
        var client = factory.CreateClientWithDatabase();
        var a = await CreateCompanyAsync(client, "A");
        var b = await CreateCompanyAsync(client, "B");
        var a1 = await CreateAssessmentAsync(client, a, _ => 2);
        var a2 = await CreateAssessmentAsync(client, a, _ => 3);
        var b1 = await CreateAssessmentAsync(client, b, _ => 3);

        var ok = await client.GetAsync($"/api/assessments/{a1}/compare/{a2}");
        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
        var json = await ok.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(25m, json.GetProperty("totalChange").GetDecimal());

        var foreign = await client.GetAsync($"/api/assessments/{a1}/compare/{b1}");
        Assert.Equal(HttpStatusCode.BadRequest, foreign.StatusCode);
        var self = await client.GetAsync($"/api/assessments/{a1}/compare/{a1}");
        Assert.Equal(HttpStatusCode.BadRequest, self.StatusCode);

        var missing = await client.GetAsync($"/api/assessments/{a1}/compare/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
    }

    [Fact]
    public async Task KarbonHedefi_KurOkuSil_VeGelisimdeGorunur()
    {
        using var factory = new ApiFactory();
        var client = factory.CreateClientWithDatabase();
        var companyId = await CreateCompanyAsync(client);
        var url = $"/api/companies/{companyId}/carbon-target";

        // Hedef yokken
        var none = await client.GetAsync(url);
        Assert.Equal(HttpStatusCode.NotFound, none.StatusCode);

        // Baz yıl kaydı olmadan kurulamaz
        var noBase = await client.PutAsJsonAsync(url, new { baseYear = 2023, targetYear = 2030, reductionPercent = 20 });
        Assert.Equal(HttpStatusCode.BadRequest, noBase.StatusCode);

        (await AddFootprintAsync(client, companyId, 2023, 100_000m)).EnsureSuccessStatusCode(); // 47,8 t
        (await AddFootprintAsync(client, companyId, 2025, 90_000m)).EnsureSuccessStatusCode();  // 43,02 t → %10

        var put = await client.PutAsJsonAsync(url, new { baseYear = 2023, targetYear = 2030, reductionPercent = 20 });
        Assert.Equal(HttpStatusCode.OK, put.StatusCode);
        var target = await put.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("OnTrack", target.GetProperty("status").GetString());
        Assert.Equal(10.0m, target.GetProperty("achievedReductionPercent").GetDecimal());
        Assert.Equal(38.24m, target.GetProperty("targetTonnes").GetDecimal());

        var progress = await client.GetFromJsonAsync<JsonElement>($"/api/companies/{companyId}/progress");
        Assert.Equal("OnTrack", progress.GetProperty("carbon").GetProperty("target").GetProperty("status").GetString());
        Assert.Equal(-10.0m, progress.GetProperty("carbon").GetProperty("changeSinceBasePercent").GetDecimal());

        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync(url)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync(url)).StatusCode);
    }

    [Fact]
    public async Task KarbonHedefi_GecersizYuzde_400_FirmaYok_404()
    {
        using var factory = new ApiFactory();
        var client = factory.CreateClientWithDatabase();
        var companyId = await CreateCompanyAsync(client);

        var bad = await client.PutAsJsonAsync($"/api/companies/{companyId}/carbon-target",
            new { baseYear = 2023, targetYear = 2030, reductionPercent = 150 });
        Assert.Equal(HttpStatusCode.BadRequest, bad.StatusCode);

        var missing = await client.PutAsJsonAsync($"/api/companies/{Guid.NewGuid()}/carbon-target",
            new { baseYear = 2023, targetYear = 2030, reductionPercent = 20 });
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
    }
}
