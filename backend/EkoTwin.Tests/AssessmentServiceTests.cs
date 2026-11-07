using EkoTwin.Dtos;
using EkoTwin.Models;
using EkoTwin.Services;
using EkoTwin.Tests.Helpers;
using Microsoft.EntityFrameworkCore;

namespace EkoTwin.Tests;

/// <summary>
/// Değerlendirme servisinin testleri: kayıt, skor hesaplama ve yanıt doğrulama kuralları.
/// Her test kendi boş InMemory veritabanıyla çalışır (16 soru hazır gelir).
/// </summary>
public class AssessmentServiceTests
{
    // ---------------- Başarılı senaryolar ----------------

    [Fact]
    public async Task CreateAsync_GecerliYanitlarla_SkorlariHesaplarVeKaydeder()
    {
        using var db = TestDb.Create();
        var company = await TestDb.AddCompanyAsync(db);
        var service = new AssessmentService(db);

        // Dijital (1-4): 5 | Enerji (5-8): 3 | Karbon (9-12): 1 | Tedarik (13-16): 2,3,4,5
        var request = TestDb.BuildRequest(db, company.Id, n => n switch
        {
            <= 4 => 5,
            <= 8 => 3,
            <= 12 => 1,
            _ => n - 11 // 13→2, 14→3, 15→4, 16→5
        });

        var result = await service.CreateAsync(request);

        Assert.Equal(53.13m, result.TotalScore);
        Assert.Equal("Gelişen", result.MaturityLevel);
        Assert.Equal(company.CompanyName, result.CompanyName);

        var scores = result.DimensionScores.ToDictionary(d => d.Dimension, d => d.Score);
        Assert.Equal(100m, scores[Dimension.DigitalInfrastructure]);
        Assert.Equal(50m, scores[Dimension.EnergyEfficiency]);
        Assert.Equal(0m, scores[Dimension.CarbonManagement]);
        Assert.Equal(62.5m, scores[Dimension.SupplyChain]);

        // Veritabanına gerçekten yazıldı mı?
        Assert.Equal(1, await db.Assessments.CountAsync());
        Assert.Equal(16, await db.AssessmentDetails.CountAsync());
    }

    [Fact]
    public async Task CreateAsync_AbyeIhracatYapanMetalFirmasi_KarbonSkoruDusukse_CbamRiskiYuksek()
    {
        using var db = TestDb.Create();
        var company = await TestDb.AddCompanyAsync(db, "Elazığ Döküm", exportsToEu: true,
            cbamProductGroup: CbamProductGroup.IronAndSteel);
        var service = new AssessmentService(db);

        // Karbon soruları (9-12) hepsi 1 → Karbon Yönetimi skoru 0
        var request = TestDb.BuildRequest(db, company.Id, n => n is >= 9 and <= 12 ? 1 : 4);

        var result = await service.CreateAsync(request);

        Assert.True(result.CbamRisk.IsInScope);
        Assert.Equal(CbamRiskEvaluator.High, result.CbamRisk.RiskLevel);
        Assert.Equal(CbamProductGroup.IronAndSteel, result.CbamRisk.ProductGroup);
    }

    [Fact]
    public async Task CreateAsync_KapsamDisiFirma_CbamKapsamDisi()
    {
        using var db = TestDb.Create();
        var company = await TestDb.AddCompanyAsync(db, "Mobilya Atölyesi", exportsToEu: true);
        var service = new AssessmentService(db);

        var result = await service.CreateAsync(TestDb.BuildRequest(db, company.Id, _ => 1));

        Assert.False(result.CbamRisk.IsInScope);
        Assert.Equal(CbamRiskEvaluator.OutOfScope, result.CbamRisk.RiskLevel);
    }

    [Fact]
    public async Task CreateAsync_YanitlariSoruSirasinaGoreDondurur()
    {
        using var db = TestDb.Create();
        var company = await TestDb.AddCompanyAsync(db);
        var service = new AssessmentService(db);

        var request = TestDb.BuildRequest(db, company.Id, _ => 3);
        request.Answers.Reverse(); // yanıtları ters sırada gönder

        var result = await service.CreateAsync(request);

        Assert.Equal(Enumerable.Range(1, 16), result.Answers.Select(a => a.DisplayOrder));
    }

    [Fact]
    public async Task CreateAsync_PasifSoruYanitlanmadanKabulEdilir()
    {
        using var db = TestDb.Create();
        var company = await TestDb.AddCompanyAsync(db);
        var service = new AssessmentService(db);

        var q16 = await db.Questions.SingleAsync(q => q.DisplayOrder == 16);
        q16.IsActive = false;
        await db.SaveChangesAsync();

        var request = TestDb.BuildRequest(db, company.Id, _ => 4); // sadece aktif 15 soru

        var result = await service.CreateAsync(request);

        Assert.Equal(15, result.Answers.Count);
    }

    // ---------------- Doğrulama hataları ----------------

    [Fact]
    public async Task CreateAsync_FirmaYoksa_KeyNotFoundFirlatir()
    {
        using var db = TestDb.Create();
        var service = new AssessmentService(db);

        var request = TestDb.BuildRequest(db, Guid.NewGuid(), _ => 3);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.CreateAsync(request));
        Assert.Equal(0, await db.Assessments.CountAsync());
    }

    [Fact]
    public async Task CreateAsync_EksikYanitta_EksikSoruNumaralariniBildirir()
    {
        using var db = TestDb.Create();
        var company = await TestDb.AddCompanyAsync(db);
        var service = new AssessmentService(db);

        var request = TestDb.BuildRequest(db, company.Id, _ => 3);
        request.Answers = request.Answers.Take(14).ToList(); // 15 ve 16 eksik

        var ex = await Assert.ThrowsAsync<AssessmentValidationException>(() => service.CreateAsync(request));

        Assert.Contains(ex.Errors, e => e.Contains("Yanıtlanmamış soru numaraları: 15, 16"));
        Assert.Equal(0, await db.Assessments.CountAsync());
    }

    [Fact]
    public async Task CreateAsync_AyniSoruIkiKezYanitlanirsa_HataVerir()
    {
        using var db = TestDb.Create();
        var company = await TestDb.AddCompanyAsync(db);
        var service = new AssessmentService(db);

        var request = TestDb.BuildRequest(db, company.Id, _ => 3);
        request.Answers.Add(new AnswerRequest { QuestionId = request.Answers[0].QuestionId, Score = 5 });

        var ex = await Assert.ThrowsAsync<AssessmentValidationException>(() => service.CreateAsync(request));

        Assert.Contains(ex.Errors, e => e.Contains("birden fazla kez"));
    }

    [Fact]
    public async Task CreateAsync_BilinmeyenSoruId_HataVerir()
    {
        using var db = TestDb.Create();
        var company = await TestDb.AddCompanyAsync(db);
        var service = new AssessmentService(db);

        var request = TestDb.BuildRequest(db, company.Id, _ => 3);
        request.Answers.Add(new AnswerRequest { QuestionId = Guid.NewGuid(), Score = 3 });

        var ex = await Assert.ThrowsAsync<AssessmentValidationException>(() => service.CreateAsync(request));

        Assert.Contains(ex.Errors, e => e.Contains("Geçersiz veya pasif soru"));
    }

    [Fact]
    public async Task CreateAsync_PasifSoruyaYanitVerilirse_HataVerir()
    {
        using var db = TestDb.Create();
        var company = await TestDb.AddCompanyAsync(db);
        var service = new AssessmentService(db);

        var request = TestDb.BuildRequest(db, company.Id, _ => 3); // 16 soru (henüz hepsi aktif)

        var q16 = await db.Questions.SingleAsync(q => q.DisplayOrder == 16);
        q16.IsActive = false; // istek hazırlandıktan sonra pasif yap
        await db.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<AssessmentValidationException>(() => service.CreateAsync(request));

        Assert.Contains(ex.Errors, e => e.Contains("Geçersiz veya pasif soru"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    [InlineData(-1)]
    public async Task CreateAsync_PuanBirIleBesDisindaysa_HataVerir(int invalidScore)
    {
        using var db = TestDb.Create();
        var company = await TestDb.AddCompanyAsync(db);
        var service = new AssessmentService(db);

        var request = TestDb.BuildRequest(db, company.Id, n => n == 1 ? invalidScore : 3);

        var ex = await Assert.ThrowsAsync<AssessmentValidationException>(() => service.CreateAsync(request));

        Assert.Contains(ex.Errors, e => e.Contains("1 ile 5 arasında"));
    }

    // ---------------- Okuma ----------------

    [Fact]
    public async Task GetByIdAsync_KayitYoksa_NullDoner()
    {
        using var db = TestDb.Create();
        var service = new AssessmentService(db);

        Assert.Null(await service.GetByIdAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task GetByCompanyAsync_FirmaYoksa_NullDoner()
    {
        using var db = TestDb.Create();
        var service = new AssessmentService(db);

        Assert.Null(await service.GetByCompanyAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task GetByCompanyAsync_YenidenEskiyeSiralar()
    {
        using var db = TestDb.Create();
        var company = await TestDb.AddCompanyAsync(db);
        var service = new AssessmentService(db);

        var first = await service.CreateAsync(TestDb.BuildRequest(db, company.Id, _ => 2));
        var second = await service.CreateAsync(TestDb.BuildRequest(db, company.Id, _ => 4));

        // Tarihleri belirgin şekilde ayır (aynı milisaniyede oluşabilirler)
        var firstEntity = await db.Assessments.SingleAsync(a => a.Id == first.Id);
        firstEntity.CreatedAt = DateTime.UtcNow.AddDays(-30);
        await db.SaveChangesAsync();

        var list = await service.GetByCompanyAsync(company.Id);

        Assert.NotNull(list);
        Assert.Equal(new[] { second.Id, first.Id }, list!.Select(a => a.Id));
        Assert.Equal(75m, list[0].TotalScore);  // hepsi 4 → 75
        Assert.Equal(25m, list[1].TotalScore);  // hepsi 2 → 25
    }
}
