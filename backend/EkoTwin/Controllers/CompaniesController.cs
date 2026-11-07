using EkoTwin.Data;
using EkoTwin.Dtos;
using EkoTwin.Models;
using EkoTwin.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EkoTwin.Controllers;

[ApiController]
[Route("api/companies")]
public class CompaniesController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IAssessmentService _assessments;

    public CompaniesController(AppDbContext db, IAssessmentService assessments)
    {
        _db = db;
        _assessments = assessments;
    }

    /// <summary>Tüm firmaları listeler (en yeni önce).</summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<CompanyResponse>>> GetAll(CancellationToken ct)
    {
        var companies = await _db.Companies
            .AsNoTracking()
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new CompanyResponse(c.Id, c.CompanyName, c.Sector, c.EmployeeCount, c.CreatedAt))
            .ToListAsync(ct);

        return Ok(companies);
    }

    /// <summary>Tek bir firmayı getirir.</summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CompanyResponse>> GetById(Guid id, CancellationToken ct)
    {
        var company = await _db.Companies
            .AsNoTracking()
            .Where(c => c.Id == id)
            .Select(c => new CompanyResponse(c.Id, c.CompanyName, c.Sector, c.EmployeeCount, c.CreatedAt))
            .FirstOrDefaultAsync(ct);

        return company is null ? NotFound() : Ok(company);
    }

    /// <summary>Yeni firma kaydeder. Id'yi veritabanı (uuidv7) üretir.</summary>
    [HttpPost]
    public async Task<ActionResult<CompanyResponse>> Create(CreateCompanyRequest request, CancellationToken ct)
    {
        var company = new Company
        {
            CompanyName = request.CompanyName.Trim(),
            Sector = request.Sector.Trim(),
            EmployeeCount = request.EmployeeCount,
            CreatedAt = DateTime.UtcNow
        };

        _db.Companies.Add(company);
        await _db.SaveChangesAsync(ct);

        var response = new CompanyResponse(company.Id, company.CompanyName, company.Sector, company.EmployeeCount, company.CreatedAt);
        return CreatedAtAction(nameof(GetById), new { id = company.Id }, response);
    }

    /// <summary>Firmanın geçmiş değerlendirmelerini listeler (gelişimi izlemek için).</summary>
    [HttpGet("{id:guid}/assessments")]
    public async Task<ActionResult<IEnumerable<AssessmentSummaryResponse>>> GetAssessments(Guid id, CancellationToken ct)
    {
        var result = await _assessments.GetByCompanyAsync(id, ct);
        return result is null ? NotFound() : Ok(result);
    }
}
