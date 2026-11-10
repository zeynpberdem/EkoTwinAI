using EkoTwin.Dtos;
using EkoTwin.Services.Narrative;
using Microsoft.AspNetCore.Mvc;

namespace EkoTwin.Controllers;

/// <summary>
/// Rapor mektubu: değerlendirme sonuçlarını KOBİ sahibine sade bir mektup olarak anlatır.
/// Yapay zekâ yalnızca anlatır; tüm rakamlar kuralların sonucudur ve metin otomatik kontrolden geçer.
/// </summary>
[ApiController]
[Route("api/assessments/{assessmentId:guid}/narrative")]
public class NarrativeController : ControllerBase
{
    private readonly INarrativeService _service;

    public NarrativeController(INarrativeService service)
    {
        _service = service;
    }

    /// <summary>Yeni mektup üretir (yapay zekâ ayarlı değilse kural tabanlı metin).</summary>
    [HttpPost]
    public async Task<ActionResult<NarrativeResponse>> Generate(Guid assessmentId, CancellationToken ct)
    {
        var result = await _service.GenerateAsync(assessmentId, ct);
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>En son üretilen mektubu getirir.</summary>
    [HttpGet]
    public async Task<ActionResult<NarrativeResponse>> GetLatest(Guid assessmentId, CancellationToken ct)
    {
        var result = await _service.GetLatestAsync(assessmentId, ct);
        return result is null ? NotFound() : Ok(result);
    }
}
