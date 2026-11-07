using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EkoTwin.Dtos;
using EkoTwin.Services;
using EkoTwin.Tests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;

namespace EkoTwin.Tests;

/// <summary>
/// Uygulamayı HTTP üzerinden uçtan uca test eder: sağlık kontrolü, CORS, hata yönetimi,
/// firma CRUD uç noktaları ve açılır liste uç noktaları.
/// </summary>
public class ApiIntegrationTests
{
    private const string AllowedOrigin = "http://localhost:5173";

    // ===================== A1: Sağlık kontrolü =====================

    [Fact]
    public async Task Health_UygulamaVeVeritabaniAyaktaysa_200Healthy()
    {
        using var factory = new ApiFactory();
        var client = factory.CreateClientWithDatabase();

        var response = await client.GetAsync("/health");
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", json.GetProperty("status").GetString());
        Assert.Equal("database", json.GetProperty("checks")[0].GetProperty("name").GetString());
    }

    // ===================== A1: CORS =====================

    [Fact]
    public async Task Cors_IzinliAdrestenGelenOnIstek_IzinBasligiAlir()
    {
        using var factory = new ApiFactory();
        var client = factory.CreateClientWithDatabase();

        var preflight = new HttpRequestMessage(HttpMethod.Options, "/api/companies");
        preflight.Headers.Add("Origin", AllowedOrigin);
        preflight.Headers.Add("Access-Control-Request-Method", "POST");
        preflight.Headers.Add("Access-Control-Request-Headers", "content-type");

        var response = await client.SendAsync(preflight);

        Assert.True(response.Headers.TryGetValues("Access-Control-Allow-Origin", out var values));
        Assert.Equal(AllowedOrigin, values!.Single());
    }

    [Fact]
    public async Task Cors_IzinsizAdres_IzinBasligiAlmaz()
    {
        using var factory = new ApiFactory();
        var client = factory.CreateClientWithDatabase();

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/questions");
        request.Headers.Add("Origin", "http://kotu-site.example");

        var response = await client.SendAsync(request);

        Assert.False(response.Headers.Contains("Access-Control-Allow-Origin"));
    }

    // ===================== A1: Hata yönetimi =====================

    [Fact]
    public async Task BeklenmeyenHata_500VeTurkceProblemDetailsDoner()
    {
        using var factory = new ApiFactory
        {
            ConfigureTestServices = s => ReplaceAssessmentService(s, new InvalidOperationException("test hatası"))
        };
        var client = factory.CreateClientWithDatabase();

        var response = await client.GetAsync($"/api/assessments/{Guid.NewGuid()}");
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal("Beklenmeyen bir hata oluştu", problem.GetProperty("title").GetString());
        // Geliştirme ortamında teknik ayrıntı eklenir
        Assert.Contains("test hatası", problem.GetProperty("exception").GetString());
    }

    [Fact]
    public async Task VeritabaniHatasi_503Doner()
    {
        using var factory = new ApiFactory
        {
            ConfigureTestServices = s => ReplaceAssessmentService(s, new NpgsqlException("bağlantı yok"))
        };
        var client = factory.CreateClientWithDatabase();

        var response = await client.GetAsync($"/api/assessments/{Guid.NewGuid()}");
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal("Veritabanına ulaşılamıyor", problem.GetProperty("title").GetString());
    }

    [Fact]
    public async Task OlmayanAdres_404ProblemDetailsDoner()
    {
        using var factory = new ApiFactory();
        var client = factory.CreateClientWithDatabase();

        var response = await client.GetAsync("/api/boyle-bir-adres-yok");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    // ===================== A2: Firma CRUD =====================

    [Fact]
    public async Task Firma_EkleGuncelleListeleSil_TumAkisCalisir()
    {
        using var factory = new ApiFactory();
        var client = factory.CreateClientWithDatabase();

        // Ekle
        var create = await client.PostAsJsonAsync("/api/companies", new
        {
            companyName = "Harput Döküm",
            sector = "MetalWorkingAndCasting",
            subSector = "Pik döküm",
            employeeCount = 40,
            exportsToEu = true,
            cbamProductGroup = "IronAndSteel"
        });
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var created = await create.Content.ReadFromJsonAsync<JsonElement>();
        var id = created.GetProperty("id").GetString();
        Assert.Equal("Metal İşleme ve Döküm", created.GetProperty("sectorName").GetString());
        Assert.Equal($"/api/companies/{id}", create.Headers.Location?.AbsolutePath);

        // Güncelle
        var update = await client.PutAsJsonAsync($"/api/companies/{id}", new
        {
            companyName = "Harput Döküm A.Ş.",
            sector = "MetalWorkingAndCasting",
            employeeCount = 45,
            exportsToEu = false,
            cbamProductGroup = "IronAndSteel"
        });
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        var updated = await update.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Harput Döküm A.Ş.", updated.GetProperty("companyName").GetString());
        Assert.False(updated.GetProperty("exportsToEu").GetBoolean());

        // Sektöre göre listele
        var metal = await client.GetFromJsonAsync<JsonElement>("/api/companies?sector=MetalWorkingAndCasting");
        var food = await client.GetFromJsonAsync<JsonElement>("/api/companies?sector=FoodAndBeverage");
        Assert.Equal(1, metal.GetArrayLength());
        Assert.Equal(0, food.GetArrayLength());

        // Sil
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/companies/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/companies/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.DeleteAsync($"/api/companies/{id}")).StatusCode);
    }

    [Fact]
    public async Task Firma_OlmayanFirmayiGuncelleme_404()
    {
        using var factory = new ApiFactory();
        var client = factory.CreateClientWithDatabase();

        var response = await client.PutAsJsonAsync($"/api/companies/{Guid.NewGuid()}", new
        {
            companyName = "X", sector = "Other", employeeCount = 5
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Firma_SektorGonderilmezse_400VeTurkceMesaj()
    {
        using var factory = new ApiFactory();
        var client = factory.CreateClientWithDatabase();

        var response = await client.PostAsJsonAsync("/api/companies", new { companyName = "X", employeeCount = 5 });
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("Sektör seçilmelidir.", problem.GetProperty("errors").GetProperty("Sector")[0].GetString());
    }

    [Theory]
    [InlineData("\"Plastik\"")]  // listede olmayan ad
    [InlineData("500")]          // tanımsız sayı
    public async Task Firma_GecersizSektor_400(string sectorJson)
    {
        using var factory = new ApiFactory();
        var client = factory.CreateClientWithDatabase();

        var body = $$"""{ "companyName": "X", "sector": {{sectorJson}}, "employeeCount": 5 }""";
        var response = await client.PostAsync("/api/companies",
            new StringContent(body, System.Text.Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ===================== A2: Açılır listeler =====================

    [Fact]
    public async Task Lookups_SektorListesi_12SektoruTurkceAdlariylaDoner()
    {
        using var factory = new ApiFactory();
        var client = factory.CreateClientWithDatabase();

        var sectors = await client.GetFromJsonAsync<List<LookupItem>>("/api/lookups/sectors");

        Assert.NotNull(sectors);
        Assert.Equal(12, sectors!.Count);
        Assert.Contains(sectors, s => s.Value == "FurnitureAndWood" && s.Name == "Mobilya ve Ağaç Ürünleri");
        Assert.Contains(sectors, s => s.Value == "Other" && s.Name == "Diğer");
    }

    [Fact]
    public async Task Lookups_CbamVeBoyutListeleri_Doner()
    {
        using var factory = new ApiFactory();
        var client = factory.CreateClientWithDatabase();

        var groups = await client.GetFromJsonAsync<List<LookupItem>>("/api/lookups/cbam-product-groups");
        var dimensions = await client.GetFromJsonAsync<List<LookupItem>>("/api/lookups/dimensions");

        Assert.Equal(7, groups!.Count);
        Assert.Contains(groups, g => g.Value == "None" && g.Name == "Kapsam dışı ürün");
        Assert.Equal(4, dimensions!.Count);
    }

    // ===================== Yardımcılar =====================

    private static void ReplaceAssessmentService(IServiceCollection services, Exception toThrow)
    {
        services.RemoveAll<IAssessmentService>();
        services.AddScoped<IAssessmentService>(_ => new ThrowingAssessmentService(toThrow));
    }

    /// <summary>Hata yönetimini test etmek için her çağrıda verilen hatayı fırlatan sahte servis.</summary>
    private sealed class ThrowingAssessmentService(Exception toThrow) : IAssessmentService
    {
        public Task<AssessmentResponse> CreateAsync(CreateAssessmentRequest request, CancellationToken ct = default) => throw toThrow;
        public Task<AssessmentResponse?> GetByIdAsync(Guid id, CancellationToken ct = default) => throw toThrow;
        public Task<IReadOnlyList<AssessmentSummaryResponse>?> GetByCompanyAsync(Guid companyId, CancellationToken ct = default) => throw toThrow;
    }
}
