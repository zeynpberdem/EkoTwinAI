using EkoTwin.Data;
using EkoTwin.Dtos;
using EkoTwin.Models;
using Microsoft.EntityFrameworkCore;

namespace EkoTwin.Tests.Helpers;

/// <summary>
/// Testler için bellekte çalışan (InMemory) geçici veritabanı.
/// Gerçek PostgreSQL'e dokunmaz; her test kendi boş veritabanını alır.
/// Not: InMemory, SQL'e özgü kuralları (CHECK kısıtı, uuidv7()) uygulamaz;
/// bu yüzden burada iş kurallarını servis seviyesinde test ediyoruz.
/// </summary>
public static class TestDb
{
    /// <summary>16 seed soruyla birlikte yeni ve boş bir veritabanı oluşturur.</summary>
    public static AppDbContext Create()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"EkoTwinTest-{Guid.NewGuid()}")
            .Options;

        var db = new AppDbContext(options);
        db.Database.EnsureCreated(); // QuestionSeed'deki 16 soruyu ekler
        return db;
    }

    /// <summary>Veritabanına örnek bir firma ekler.</summary>
    public static async Task<Company> AddCompanyAsync(
        AppDbContext db,
        string name = "Test Tekstil A.Ş.",
        bool exportsToEu = false,
        CbamProductGroup cbamProductGroup = CbamProductGroup.None)
    {
        var company = new Company
        {
            CompanyName = name,
            Sector = SectorType.TextileAndApparel,
            EmployeeCount = 45,
            ExportsToEu = exportsToEu,
            CbamProductGroup = cbamProductGroup
        };
        db.Companies.Add(company);
        await db.SaveChangesAsync();
        return company;
    }

    /// <summary>
    /// Soru sırasına (1-16) göre puan veren bir değerlendirme isteği oluşturur.
    /// Örn: scoreByOrder = n => n &lt;= 4 ? 5 : 3
    /// </summary>
    public static CreateAssessmentRequest BuildRequest(AppDbContext db, Guid companyId, Func<int, int> scoreByOrder)
    {
        var answers = db.Questions
            .Where(q => q.IsActive)
            .OrderBy(q => q.DisplayOrder)
            .AsEnumerable()
            .Select(q => new AnswerRequest { QuestionId = q.Id, Score = scoreByOrder(q.DisplayOrder) })
            .ToList();

        return new CreateAssessmentRequest { CompanyId = companyId, Answers = answers };
    }
}
