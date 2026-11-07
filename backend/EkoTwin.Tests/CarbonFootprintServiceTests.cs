using EkoTwin.Data;
using EkoTwin.Dtos;
using EkoTwin.Models;
using EkoTwin.Services;
using EkoTwin.Tests.Helpers;
using Microsoft.EntityFrameworkCore;

namespace EkoTwin.Tests;

/// <summary>Karbon hesaplayıcı servisinin iş kuralları (kaydetme, doğrulama, karşılaştırma, snapshot, CBAM).</summary>
public class CarbonFootprintServiceTests
{
    private static readonly TimeProvider Clock = FixedTimeProvider.At(2026);

    private static CarbonFootprintService Service(AppDbContext db) => new(db, Clock);

    private static CarbonFootprintRequest Request(int year, params (EmissionSource s, decimal q)[] entries) => new()
    {
        Year = year,
        Entries = entries.Select(e => new CarbonEntryRequest { Source = e.s, Quantity = e.q }).ToList()
    };

    // ===================== Kaydetme =====================

    [Fact]
    public async Task Create_HesaplarKaydederVeRaporDoner()
    {
        await using var db = TestDb.Create();
        var company = await TestDb.AddCompanyAsync(db); // 45 çalışan

        var result = await Service(db).CreateAsync(company.Id, Request(2025,
            (EmissionSource.Electricity, 200_000m),   // 95,6 t
            (EmissionSource.NaturalGas, 10_000m)));   // 19,372 t

        Assert.Equal(95.6m, result.Scope2Tonnes);
        Assert.Equal(19.372m, result.Scope1Tonnes);
        Assert.Equal(114.972m, result.TotalTonnes);
        Assert.Equal(45, result.EmployeeCount);
        Assert.Equal(2.555m, result.TonnesPerEmployee); // 114,972 / 45 = 2,5549.. → 2,555
        Assert.Equal(EmissionSource.Electricity, result.LargestSource);
        Assert.Equal(83.2m, result.LargestSourceSharePercent);
        Assert.NotNull(result.Guidance);
        Assert.Null(result.Comparison);
        Assert.NotEmpty(result.Limitations);

        Assert.Equal(1, await db.CarbonFootprints.CountAsync());
        Assert.Equal(2, await db.CarbonFootprintItems.CountAsync());
    }

    [Fact]
    public async Task Create_FirmaYoksa_KeyNotFound()
    {
        await using var db = TestDb.Create();

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            Service(db).CreateAsync(Guid.NewGuid(), Request(2025, (EmissionSource.Electricity, 1000m))));
    }

    [Fact]
    public async Task Create_AyniYilIkinciKez_Cakisma()
    {
        await using var db = TestDb.Create();
        var company = await TestDb.AddCompanyAsync(db);
        var service = Service(db);

        await service.CreateAsync(company.Id, Request(2025, (EmissionSource.Electricity, 1000m)));

        var ex = await Assert.ThrowsAsync<CarbonYearConflictException>(() =>
            service.CreateAsync(company.Id, Request(2025, (EmissionSource.Electricity, 2000m))));
        Assert.Equal(2025, ex.Year);
    }

    [Fact]
    public async Task Create_FarkliFirmalarAyniYil_Serbest()
    {
        await using var db = TestDb.Create();
        var a = await TestDb.AddCompanyAsync(db, "A");
        var b = await TestDb.AddCompanyAsync(db, "B");
        var service = Service(db);

        await service.CreateAsync(a.Id, Request(2025, (EmissionSource.Electricity, 1000m)));
        await service.CreateAsync(b.Id, Request(2025, (EmissionSource.Electricity, 1000m)));

        Assert.Equal(2, await db.CarbonFootprints.CountAsync());
    }

    // ===================== Doğrulama =====================

    [Fact]
    public async Task Create_GelecekYil_Gecersiz()
    {
        await using var db = TestDb.Create();
        var company = await TestDb.AddCompanyAsync(db);

        var ex = await Assert.ThrowsAsync<CarbonValidationException>(() =>
            Service(db).CreateAsync(company.Id, Request(2027, (EmissionSource.Electricity, 1000m))));
        Assert.Contains(ex.Errors, e => e.Contains("Gelecek yıl"));
    }

    [Fact]
    public async Task Create_BuYil_Gecerli()
    {
        await using var db = TestDb.Create();
        var company = await TestDb.AddCompanyAsync(db);

        var result = await Service(db).CreateAsync(company.Id, Request(2026, (EmissionSource.Electricity, 1000m)));
        Assert.Equal(2026, result.Year);
    }

    [Fact]
    public async Task Create_TekrarliKaynak_Gecersiz()
    {
        await using var db = TestDb.Create();
        var company = await TestDb.AddCompanyAsync(db);

        var ex = await Assert.ThrowsAsync<CarbonValidationException>(() =>
            Service(db).CreateAsync(company.Id, Request(2025,
                (EmissionSource.Diesel, 100m), (EmissionSource.Diesel, 200m))));
        Assert.Contains(ex.Errors, e => e.Contains("Motorin"));
    }

    [Fact]
    public async Task Create_HepsiSifir_Gecersiz()
    {
        await using var db = TestDb.Create();
        var company = await TestDb.AddCompanyAsync(db);

        var ex = await Assert.ThrowsAsync<CarbonValidationException>(() =>
            Service(db).CreateAsync(company.Id, Request(2025, (EmissionSource.Electricity, 0m))));
        Assert.Contains(ex.Errors, e => e.Contains("sıfırdan büyük"));
    }

    [Fact]
    public async Task Create_NegatifMiktar_Gecersiz()
    {
        await using var db = TestDb.Create();
        var company = await TestDb.AddCompanyAsync(db);

        var ex = await Assert.ThrowsAsync<CarbonValidationException>(() =>
            Service(db).CreateAsync(company.Id, Request(2025,
                (EmissionSource.Electricity, 1000m), (EmissionSource.Lpg, -5m))));
        Assert.Contains(ex.Errors, e => e.Contains("negatif"));
    }

    [Fact]
    public async Task Create_PasifFaktor_Gecersiz()
    {
        await using var db = TestDb.Create();
        var company = await TestDb.AddCompanyAsync(db);
        var lignite = await db.EmissionFactors.SingleAsync(f => f.Source == EmissionSource.Lignite);
        lignite.IsActive = false;
        await db.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<CarbonValidationException>(() =>
            Service(db).CreateAsync(company.Id, Request(2025, (EmissionSource.Lignite, 10m))));
        Assert.Contains(ex.Errors, e => e.Contains("faktörü tanımlı değil"));
    }

    // ===================== Karşılaştırma =====================

    [Fact]
    public async Task Get_OncekiYilVarsa_KarsilastirmaGosterir()
    {
        await using var db = TestDb.Create();
        var company = await TestDb.AddCompanyAsync(db);
        var service = Service(db);

        await service.CreateAsync(company.Id, Request(2024, (EmissionSource.Electricity, 100_000m))); // 47,8 t
        var current = await service.CreateAsync(company.Id, Request(2025, (EmissionSource.Electricity, 80_000m))); // 38,24 t

        Assert.NotNull(current.Comparison);
        Assert.Equal(2024, current.Comparison!.PreviousYear);
        Assert.Equal(-9.56m, current.Comparison.ChangeTonnes);
        Assert.Equal(-20.0m, current.Comparison.ChangePercent);
        Assert.Contains("azaldı", current.Comparison.Summary);
        Assert.Contains("%20", current.Comparison.Summary);
    }

    [Fact]
    public async Task Get_EnYakinOncekiYilIleKarsilastirir()
    {
        await using var db = TestDb.Create();
        var company = await TestDb.AddCompanyAsync(db);
        var service = Service(db);

        await service.CreateAsync(company.Id, Request(2021, (EmissionSource.Electricity, 1000m)));
        await service.CreateAsync(company.Id, Request(2023, (EmissionSource.Electricity, 2000m)));
        var current = await service.CreateAsync(company.Id, Request(2025, (EmissionSource.Electricity, 3000m)));

        Assert.Equal(2023, current.Comparison!.PreviousYear);
        Assert.Contains("arttı", current.Comparison.Summary);
    }

    [Fact]
    public async Task ListByCompany_YeniYilOnce_FirmaYoksaNull()
    {
        await using var db = TestDb.Create();
        var company = await TestDb.AddCompanyAsync(db);
        var service = Service(db);

        await service.CreateAsync(company.Id, Request(2023, (EmissionSource.Electricity, 1000m)));
        await service.CreateAsync(company.Id, Request(2025, (EmissionSource.Electricity, 1000m)));
        await service.CreateAsync(company.Id, Request(2024, (EmissionSource.Electricity, 1000m)));

        var list = await service.ListByCompanyAsync(company.Id);
        Assert.Equal(new[] { 2025, 2024, 2023 }, list!.Select(x => x.Year));

        Assert.Null(await service.ListByCompanyAsync(Guid.NewGuid()));
    }

    // ===================== Snapshot ve güncelleme =====================

    [Fact]
    public async Task FaktorSonradanDegisirse_EskiHesapDegismez_GuncellemeYeniFaktoruKullanir()
    {
        await using var db = TestDb.Create();
        var company = await TestDb.AddCompanyAsync(db);
        var service = Service(db);

        var created = await service.CreateAsync(company.Id, Request(2025, (EmissionSource.Electricity, 100_000m)));
        Assert.Equal(47.8m, created.TotalTonnes);

        // Bakanlık yeni faktör yayımladı (örnek değer)
        var electricity = await db.EmissionFactors.SingleAsync(f => f.Source == EmissionSource.Electricity);
        electricity.KgCo2ePerUnit = 0.400m;
        await db.SaveChangesAsync();

        var reread = await service.GetAsync(created.Id);
        Assert.Equal(47.8m, reread!.TotalTonnes);
        Assert.Equal(0.478m, reread.Items.Single().FactorKgCo2ePerUnit);

        var updated = await service.UpdateAsync(created.Id, Request(2025, (EmissionSource.Electricity, 100_000m)));
        Assert.Equal(40.0m, updated!.TotalTonnes);
        Assert.NotNull(updated.UpdatedAt);
    }

    [Fact]
    public async Task Update_SatirlariDegistirir_EskiSatirKalmaz()
    {
        await using var db = TestDb.Create();
        var company = await TestDb.AddCompanyAsync(db);
        var service = Service(db);

        var created = await service.CreateAsync(company.Id, Request(2025,
            (EmissionSource.Electricity, 1000m), (EmissionSource.Diesel, 100m)));

        var updated = await service.UpdateAsync(created.Id, Request(2025, (EmissionSource.Lpg, 500m)));

        Assert.Equal(EmissionSource.Lpg, updated!.Items.Single().Source);
        Assert.Equal(1, await db.CarbonFootprintItems.CountAsync());
        Assert.Equal(0m, updated.Scope2Tonnes);
    }

    [Fact]
    public async Task Update_BaskaKayitlaAyniYilaTasima_Cakisma()
    {
        await using var db = TestDb.Create();
        var company = await TestDb.AddCompanyAsync(db);
        var service = Service(db);

        await service.CreateAsync(company.Id, Request(2024, (EmissionSource.Electricity, 1000m)));
        var y2025 = await service.CreateAsync(company.Id, Request(2025, (EmissionSource.Electricity, 1000m)));

        await Assert.ThrowsAsync<CarbonYearConflictException>(() =>
            service.UpdateAsync(y2025.Id, Request(2024, (EmissionSource.Electricity, 1000m))));
    }

    [Fact]
    public async Task Update_KayitYoksa_Null_Delete_KayitYoksaFalse()
    {
        await using var db = TestDb.Create();
        var service = Service(db);

        Assert.Null(await service.UpdateAsync(Guid.NewGuid(), Request(2025, (EmissionSource.Electricity, 1m))));
        Assert.False(await service.DeleteAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task Delete_KaydiVeSatirlariSiler()
    {
        await using var db = TestDb.Create();
        var company = await TestDb.AddCompanyAsync(db);
        var service = Service(db);
        var created = await service.CreateAsync(company.Id, Request(2025, (EmissionSource.Electricity, 1000m)));

        Assert.True(await service.DeleteAsync(created.Id));
        Assert.Null(await service.GetAsync(created.Id));
        Assert.Equal(0, await db.CarbonFootprintItems.CountAsync());
    }

    // ===================== CBAM notu =====================

    [Theory]
    [InlineData(true, CbamProductGroup.IronAndSteel, true)]
    [InlineData(false, CbamProductGroup.IronAndSteel, false)]
    [InlineData(true, CbamProductGroup.None, false)]
    public async Task CbamNotu_YalnizcaKapsamdakiIhracatciya(bool exportsToEu, CbamProductGroup group, bool expectNote)
    {
        await using var db = TestDb.Create();
        var company = await TestDb.AddCompanyAsync(db, exportsToEu: exportsToEu, cbamProductGroup: group);

        var result = await Service(db).CreateAsync(company.Id, Request(2025, (EmissionSource.Coke, 50m)));

        Assert.Equal(expectNote, result.CbamNote is not null);
    }

    [Fact]
    public async Task GetFactors_AktifFaktorleriDoner()
    {
        await using var db = TestDb.Create();

        var factors = await Service(db).GetFactorsAsync();

        Assert.Equal(7, factors.Count);
        Assert.Equal("Elektrik (şebekeden)", factors[0].SourceName);
        Assert.Equal("Kapsam 2 (satın alınan elektrik)", factors[0].ScopeName);
    }
}
