using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EkoTwin.Data;
using EkoTwin.Dtos;
using EkoTwin.Models;
using EkoTwin.Services;
using EkoTwin.Tests.Helpers;
using Microsoft.EntityFrameworkCore;

namespace EkoTwin.Tests;

/// <summary>Sektör karşılaştırması servisi (veritabanıyla) ve uç noktası.</summary>
public class BenchmarkServiceTests
{
    private static readonly DateTime Day = new(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);

    /// <summary>Mobilya, 25 çalışan (küçük) firmalar ekler; her biri verilen sabit puanla test doldurur.</summary>
    private static async Task<Company> AddFirmAsync(AppDbContext db, int score, SectorType sector = SectorType.FurnitureAndWood, int employees = 25)
    {
        var c = await TestDb.AddCompanyAsync(db, $"Firma {Guid.NewGuid():N}", sector: sector, employeeCount: employees);
        await TestDb.AddAssessmentAsync(db, c.Id, _ => score, Day);
        return c;
    }

    [Fact]
    public async Task FirmaYoksa_Null()
    {
        await using var db = TestDb.Create();

        Assert.Null(await new BenchmarkService(db).GetAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task TestDoldurulmamissa_YonlendirmeMesaji()
    {
        await using var db = TestDb.Create();
        var me = await TestDb.AddCompanyAsync(db);

        var r = (await new BenchmarkService(db).GetAsync(me.Id))!;

        Assert.False(r.HasAssessment);
        Assert.Null(r.Group);
        Assert.Contains("Karşılaştırma için önce testi doldurun.", r.SimpleHighlights);
    }

    [Fact]
    public async Task BesFirmadanAz_KarsilastirmaGosterilmez()
    {
        await using var db = TestDb.Create();
        var me = await AddFirmAsync(db, 3);
        for (var i = 0; i < 4; i++) await AddFirmAsync(db, 2);

        var r = (await new BenchmarkService(db).GetAsync(me.Id))!;

        Assert.True(r.HasAssessment);
        Assert.Null(r.Group);
        Assert.Empty(r.Metrics);
        Assert.Contains(BenchmarkCalculator.NotEnoughPeers, r.SimpleHighlights);
    }

    [Fact]
    public async Task BesBenzerFirma_AyniSektorVeBuyuklukleKarsilastirir()
    {
        await using var db = TestDb.Create();
        var me = await AddFirmAsync(db, 4);                       // tüm boyutlar 75
        foreach (var s in new[] { 2, 2, 3, 3, 5 }) await AddFirmAsync(db, s); // 25, 25, 50, 50, 100
        await AddFirmAsync(db, 1, SectorType.FoodAndBeverage);   // başka sektör: gruba girmez

        var r = (await new BenchmarkService(db).GetAsync(me.Id))!;

        Assert.Equal(SizeClass.Small, r.SizeClass);
        Assert.Equal(BenchmarkGroupLevel.SectorAndSize, r.Group!.Level);
        Assert.Equal(5, r.Group.PeerCount);
        var total = r.Metrics[0];
        Assert.Equal(50m, total.GroupAverage);
        Assert.Equal(8, total.BetterThanOutOf10); // 5 firmadan 4'ü düşük → %80
        Assert.Equal(BenchmarkPosition.Above, total.Position);
        Assert.Contains(r.Group.SimpleDescription, r.SimpleHighlights);
    }

    [Fact]
    public async Task HerFirmanin_EnSonTestiSayilir()
    {
        await using var db = TestDb.Create();
        var me = await AddFirmAsync(db, 3);
        var peers = new List<Company>();
        for (var i = 0; i < 5; i++) peers.Add(await AddFirmAsync(db, 1));
        // Bir firma daha sonra yeniden test doldurur ve 5'e çıkar
        await TestDb.AddAssessmentAsync(db, peers[0].Id, _ => 5, Day.AddMonths(3));

        var r = (await new BenchmarkService(db).GetAsync(me.Id))!;

        Assert.Equal(5, r.Group!.PeerCount);           // aynı firma iki kez sayılmaz
        Assert.Equal(20m, r.Metrics[0].GroupAverage);  // (100 + 0×4) / 5
    }

    [Fact]
    public async Task KarbonKarsilastirmasi_EnAz5FirmaVarsa()
    {
        await using var db = TestDb.Create();
        var me = await AddFirmAsync(db, 3);
        await TestDb.AddFootprintAsync(db, me.Id, 2025, 100m, perEmployee: 4m);
        foreach (var t in new[] { 2m, 6m, 8m, 10m, 12m })
        {
            var p = await AddFirmAsync(db, 3);
            await TestDb.AddFootprintAsync(db, p.Id, 2025, t * 25, perEmployee: t);
        }

        var r = (await new BenchmarkService(db).GetAsync(me.Id))!;

        Assert.NotNull(r.Carbon);
        Assert.Equal(8, r.Carbon!.LowerThanOutOf10);   // 6, 8, 10, 12 → 4/5
        Assert.Equal(BenchmarkPosition.Above, r.Carbon.Position);
    }

    [Fact]
    public async Task TuikReferanslari_KucukFirmayaKendiBuyuklugununDegerleri_EslesenlerCevabaGore()
    {
        await using var db = TestDb.Create();
        var me = await AddFirmAsync(db, 4); // 25 çalışan; Soru 1 = 4 → ERP'ye "dahil", Soru 3 = 4 → güvenliğe "dahil"

        var r = (await new BenchmarkService(db).GetAsync(me.Id))!;

        // Küçük firma: ERP + güvenlik (eşleşmeli) + bulut + enerji (yalnızca bilgi)
        Assert.Equal(4, r.NationalReferences.Count);
        var erp = r.NationalReferences.Single(n => n.Indicator.StartsWith("ERP"));
        Assert.Equal(23.6m, erp.Percent);
        Assert.True(erp.YouAreIncluded);
        Assert.Equal(81.2m, r.NationalReferences.Single(n => n.Indicator.Contains("güvenlik")).Percent);

        var energy = r.NationalReferences.Single(n => n.Indicator.StartsWith("Enerji"));
        Assert.Null(energy.YouAreIncluded); // kullanıcı kararı: şimdilik yalnızca bilgi
        Assert.Equal("Türkiye'de sizin büyüklüğünüzdeki (10-49 çalışan) firmaların yüzde 15'i enerjiyi azaltmak için yazılım veya akıllı sistem kullanıyor.",
            energy.SimpleText);
    }

    [Fact]
    public async Task DogrulanmamisReferans_Gosterilmez()
    {
        await using var db = TestDb.Create();
        var me = await AddFirmAsync(db, 4);
        foreach (var reference in db.BenchmarkReferences) reference.IsVerified = false;
        await db.SaveChangesAsync();

        var r = (await new BenchmarkService(db).GetAsync(me.Id))!;

        Assert.Empty(r.NationalReferences);
    }

    [Fact]
    public async Task OrtaBuyuklukFirmaya_OrtaBuyuklugunDegerleri_MikroFirmayaHicbiri()
    {
        await using var db = TestDb.Create();
        var medium = await AddFirmAsync(db, 3, employees: 120);
        var micro = await AddFirmAsync(db, 3, employees: 6);
        var service = new BenchmarkService(db);

        var m = (await service.GetAsync(medium.Id))!;
        var mi = (await service.GetAsync(micro.Id))!;

        Assert.Equal(46.0m, m.NationalReferences.Single(n => n.Indicator.StartsWith("ERP")).Percent);
        Assert.Equal(90.5m, m.NationalReferences.Single(n => n.Indicator.Contains("güvenlik")).Percent);
        Assert.Empty(mi.NationalReferences); // TÜİK araştırması 10+ çalışanı kapsar
    }

    [Fact]
    public async Task Api_200VeFirmaYoksa404()
    {
        using var factory = new ApiFactory();
        var client = factory.CreateClientWithDatabase();

        var created = await (await client.PostAsJsonAsync("/api/companies", new
        {
            companyName = "Karşılaştırma Mobilya", sector = "FurnitureAndWood", employeeCount = 20,
            exportsToEu = false, cbamProductGroup = "None"
        })).Content.ReadFromJsonAsync<JsonElement>();

        var ok = await client.GetAsync($"/api/companies/{created.GetProperty("id").GetString()}/benchmark");
        var json = await ok.Content.ReadFromJsonAsync<JsonElement>();
        var missing = await client.GetAsync($"/api/companies/{Guid.NewGuid()}/benchmark");

        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
        Assert.Equal("Small", json.GetProperty("sizeClass").GetString());
        Assert.False(json.GetProperty("hasAssessment").GetBoolean());
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
    }
}
