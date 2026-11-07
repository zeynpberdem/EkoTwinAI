using EkoTwin.Dtos;
using EkoTwin.Models;
using EkoTwin.Services;
using Microsoft.AspNetCore.Mvc;

namespace EkoTwin.Controllers;

[ApiController]
[Route("api/companies")]
public class CompaniesController : ControllerBase
{
    private readonly ICompanyService _companies;
    private readonly IAssessmentService _assessments;

    public CompaniesController(ICompanyService companies, IAssessmentService assessments)
    {
        _companies = companies;
        _assessments = assessments;
    }

    /// <summary>Firmaları listeler (en yeni önce). İsteğe bağlı: ?sector=TextileAndApparel&amp;search=döküm</summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<CompanyResponse>>> GetAll(
        [FromQuery] SectorType? sector, [FromQuery] string? search, CancellationToken ct)
    {
        return Ok(await _companies.ListAsync(sector, search, ct));
    }

    /// <summary>Tek bir firmayı getirir.</summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CompanyResponse>> GetById(Guid id, CancellationToken ct)
    {
        var company = await _companies.GetAsync(id, ct);
        return company is null ? NotFound() : Ok(company);
    }

    /// <summary>Yeni firma kaydeder. Id'yi veritabanı (uuidv7) üretir.</summary>
    [HttpPost]
    public async Task<ActionResult<CompanyResponse>> Create(CompanyRequest request, CancellationToken ct)
    {
        var company = await _companies.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = company.Id }, company);
    }

    /// <summary>Firma bilgilerini günceller (tüm alanlar gönderilir).</summary>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<CompanyResponse>> Update(Guid id, CompanyRequest request, CancellationToken ct)
    {
        var company = await _companies.UpdateAsync(id, request, ct);
        return company is null ? NotFound() : Ok(company);
    }

    /// <summary>Firmayı ve tüm değerlendirmelerini kalıcı olarak siler.</summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        return await _companies.DeleteAsync(id, ct) ? NoContent() : NotFound();
    }

    /// <summary>Firmanın geçmiş değerlendirmelerini listeler (gelişimi izlemek için).</summary>
    [HttpGet("{id:guid}/assessments")]
    public async Task<ActionResult<IEnumerable<AssessmentSummaryResponse>>> GetAssessments(Guid id, CancellationToken ct)
    {
        var result = await _assessments.GetByCompanyAsync(id, ct);
        return result is null ? NotFound() : Ok(result);
    }
}
