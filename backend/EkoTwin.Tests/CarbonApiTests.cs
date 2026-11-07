using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EkoTwin.Tests.Helpers;

namespace EkoTwin.Tests;

/// <summary>Karbon hesaplayıcı uç noktalarını HTTP üzerinden uçtan uca test eder.</summary>
public class CarbonApiTests
{
    private static async Task<string> CreateCompanyAsync(HttpClient client, bool exportsToEu = true, string group = "IronAndSteel")
    {
        var response = await client.PostAsJsonAsync("/api/companies", new
        {
            companyName = "Karbon Döküm Ltd.",
            sector = "MetalWorkingAndCasting",
            employeeCount = 50,
            exportsToEu,
            cbamProductGroup = group
        });
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        return json.GetProperty("id").GetString()!;
    }

    private static object Body(int year, params (string source, decimal quantity)[] entries) => new
    {
        year,
        entries = entries.Select(e => new { source = e.source, quantity = e.quantity })
    };

    [Fact]
    public async Task EmissionFactors_7KaynakDoner()
    {
        using var factory = new ApiFactory();
        var client = factory.CreateClientWithDatabase();

        var json = await client.GetFromJsonAsync<JsonElement>("/api/emission-factors");

        Assert.Equal(7, json.GetArrayLength());
        Assert.Equal("Electricity", json[0].GetProperty("source").GetString());
        Assert.Equal("kWh", json[0].GetProperty("unit").GetString());
    }

    [Fact]
    public async Task KarbonAkisi_OlusturOkuListeleGuncelleSil()
    {
        using var factory = new ApiFactory();
        var client = factory.CreateClientWithDatabase();
        var companyId = await CreateCompanyAsync(client);

        // Oluştur
        var create = await client.PostAsJsonAsync($"/api/companies/{companyId}/carbon-footprints",
            Body(2025, ("Electricity", 100_000m), ("Coke", 100m)));
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var created = await create.Content.ReadFromJsonAsync<JsonElement>();
        var id = created.GetProperty("id").GetString();

        Assert.Equal(47.8m, created.GetProperty("scope2Tonnes").GetDecimal());
        Assert.Equal(303.651m, created.GetProperty("scope1Tonnes").GetDecimal());
        Assert.Equal("Coke", created.GetProperty("largestSource").GetString());
        Assert.Equal(JsonValueKind.String, created.GetProperty("cbamNote").ValueKind); // demir-çelik ihracatçısı
        Assert.EndsWith($"/api/carbon-footprints/{id}", create.Headers.Location!.ToString());

        // Oku
        var get = await client.GetFromJsonAsync<JsonElement>($"/api/carbon-footprints/{id}");
        Assert.Equal(created.GetProperty("totalTonnes").GetDecimal(), get.GetProperty("totalTonnes").GetDecimal());

        // Listele
        var list = await client.GetFromJsonAsync<JsonElement>($"/api/companies/{companyId}/carbon-footprints");
        Assert.Equal(1, list.GetArrayLength());

        // Güncelle
        var put = await client.PutAsJsonAsync($"/api/carbon-footprints/{id}", Body(2025, ("Electricity", 50_000m)));
        Assert.Equal(HttpStatusCode.OK, put.StatusCode);
        var updated = await put.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(23.9m, updated.GetProperty("totalTonnes").GetDecimal());

        // Sil
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/carbon-footprints/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/carbon-footprints/{id}")).StatusCode);
    }

    [Fact]
    public async Task AyniYil_409()
    {
        using var factory = new ApiFactory();
        var client = factory.CreateClientWithDatabase();
        var companyId = await CreateCompanyAsync(client);
        var url = $"/api/companies/{companyId}/carbon-footprints";

        await client.PostAsJsonAsync(url, Body(2025, ("Electricity", 1000m)));
        var second = await client.PostAsJsonAsync(url, Body(2025, ("Electricity", 1000m)));

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        var problem = await second.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Bu yıl için kayıt var", problem.GetProperty("title").GetString());
    }

    [Fact]
    public async Task GecersizGirdiler_400()
    {
        using var factory = new ApiFactory();
        var client = factory.CreateClientWithDatabase();
        var companyId = await CreateCompanyAsync(client);
        var url = $"/api/companies/{companyId}/carbon-footprints";

        // Bilinmeyen kaynak (model doğrulaması / JSON)
        var unknown = await client.PostAsJsonAsync(url, Body(2025, ("Uranium", 1m)));
        Assert.Equal(HttpStatusCode.BadRequest, unknown.StatusCode);

        // Boş liste
        var empty = await client.PostAsJsonAsync(url, new { year = 2025, entries = Array.Empty<object>() });
        Assert.Equal(HttpStatusCode.BadRequest, empty.StatusCode);

        // İş kuralı: hepsi sıfır
        var zero = await client.PostAsJsonAsync(url, Body(2025, ("Electricity", 0m)));
        Assert.Equal(HttpStatusCode.BadRequest, zero.StatusCode);
        var problem = await zero.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(problem.GetProperty("errors").TryGetProperty("Entries", out _));
    }

    [Fact]
    public async Task FirmaYoksa_404()
    {
        using var factory = new ApiFactory();
        var client = factory.CreateClientWithDatabase();

        var post = await client.PostAsJsonAsync($"/api/companies/{Guid.NewGuid()}/carbon-footprints",
            Body(2025, ("Electricity", 1000m)));
        var list = await client.GetAsync($"/api/companies/{Guid.NewGuid()}/carbon-footprints");

        Assert.Equal(HttpStatusCode.NotFound, post.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, list.StatusCode);
    }
}
