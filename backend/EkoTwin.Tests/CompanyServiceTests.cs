using EkoTwin.Dtos;
using EkoTwin.Models;
using EkoTwin.Services;
using EkoTwin.Tests.Helpers;
using Microsoft.EntityFrameworkCore;

namespace EkoTwin.Tests;

/// <summary>Firma servisi testleri: ekleme, güncelleme, silme, listeleme ve filtreleme.</summary>
public class CompanyServiceTests
{
    private static CompanyRequest Request(
        string name = "Elazığ Döküm",
        SectorType sector = SectorType.MetalWorkingAndCasting,
        string? subSector = "Alüminyum döküm",
        int employees = 60) => new()
    {
        CompanyName = name,
        Sector = sector,
        SubSector = subSector,
        EmployeeCount = employees,
        ExportsToEu = true,
        CbamProductGroup = CbamProductGroup.Aluminium
    };

    [Fact]
    public async Task CreateAsync_FirmayiKaydederVeTurkceAdlariDoner()
    {
        using var db = TestDb.Create();
        var service = new CompanyService(db);

        var result = await service.CreateAsync(Request());

        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal("Metal İşleme ve Döküm", result.SectorName);
        Assert.Equal("Alüminyum", result.CbamProductGroupName);
        Assert.Equal(1, await db.Companies.CountAsync());
    }

    [Fact]
    public async Task CreateAsync_BosluklariTemizler_AltSektorYoksaBosMetin()
    {
        using var db = TestDb.Create();
        var service = new CompanyService(db);

        var result = await service.CreateAsync(Request(name: "  Ahşap Mobilya  ", subSector: null));

        Assert.Equal("Ahşap Mobilya", result.CompanyName);
        Assert.Equal(string.Empty, result.SubSector);
    }

    [Fact]
    public async Task UpdateAsync_AlanlariGunceller_KayitTarihiniDegistirmez()
    {
        using var db = TestDb.Create();
        var service = new CompanyService(db);
        var created = await service.CreateAsync(Request());

        var updated = await service.UpdateAsync(created.Id, Request(
            name: "Elazığ Mobilya", sector: SectorType.FurnitureAndWood, subSector: "Ofis mobilyası", employees: 20));

        Assert.NotNull(updated);
        Assert.Equal("Elazığ Mobilya", updated!.CompanyName);
        Assert.Equal(SectorType.FurnitureAndWood, updated.Sector);
        Assert.Equal(20, updated.EmployeeCount);
        Assert.Equal(created.CreatedAt, updated.CreatedAt);
    }

    [Fact]
    public async Task UpdateAsync_FirmaYoksa_NullDoner()
    {
        using var db = TestDb.Create();
        var service = new CompanyService(db);

        Assert.Null(await service.UpdateAsync(Guid.NewGuid(), Request()));
    }

    [Fact]
    public async Task DeleteAsync_FirmayiSiler()
    {
        using var db = TestDb.Create();
        var service = new CompanyService(db);
        var created = await service.CreateAsync(Request());

        Assert.True(await service.DeleteAsync(created.Id));
        Assert.Null(await service.GetAsync(created.Id));
        Assert.Equal(0, await db.Companies.CountAsync());
    }

    [Fact]
    public async Task DeleteAsync_FirmaYoksa_FalseDoner()
    {
        using var db = TestDb.Create();
        var service = new CompanyService(db);

        Assert.False(await service.DeleteAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task ListAsync_SektoreGoreFiltreler()
    {
        using var db = TestDb.Create();
        var service = new CompanyService(db);
        await service.CreateAsync(Request(name: "Döküm A", sector: SectorType.MetalWorkingAndCasting));
        await service.CreateAsync(Request(name: "Mobilya B", sector: SectorType.FurnitureAndWood));
        await service.CreateAsync(Request(name: "Döküm C", sector: SectorType.MetalWorkingAndCasting));

        var metal = await service.ListAsync(sector: SectorType.MetalWorkingAndCasting);
        var all = await service.ListAsync();

        Assert.Equal(2, metal.Count);
        Assert.All(metal, c => Assert.Equal(SectorType.MetalWorkingAndCasting, c.Sector));
        Assert.Equal(3, all.Count);
    }

    [Fact]
    public async Task ListAsync_AdaGoreBuyukKucukHarfDuyarsizArar()
    {
        using var db = TestDb.Create();
        var service = new CompanyService(db);
        await service.CreateAsync(Request(name: "Harput DOKUM"));
        await service.CreateAsync(Request(name: "Keban Mobilya"));

        var result = await service.ListAsync(search: "dokum");

        Assert.Single(result);
        Assert.Equal("Harput DOKUM", result[0].CompanyName);
    }

    [Fact]
    public async Task ListAsync_EnYeniFirmayiOnceGetirir()
    {
        using var db = TestDb.Create();
        var service = new CompanyService(db);
        var older = await service.CreateAsync(Request(name: "Eski"));
        var newer = await service.CreateAsync(Request(name: "Yeni"));

        var entity = await db.Companies.SingleAsync(c => c.Id == older.Id);
        entity.CreatedAt = DateTime.UtcNow.AddDays(-10);
        await db.SaveChangesAsync();

        var list = await service.ListAsync();

        Assert.Equal(new[] { newer.Id, older.Id }, list.Select(c => c.Id));
    }
}
