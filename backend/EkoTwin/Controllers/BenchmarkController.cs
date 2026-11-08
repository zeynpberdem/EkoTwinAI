using EkoTwin.Dtos;
using EkoTwin.Services;
using Microsoft.AspNetCore.Mvc;

namespace EkoTwin.Controllers;

/// <summary>Sektör karşılaştırması: "Benzer firmalara göre neredeyim?"</summary>
[ApiController]
public class BenchmarkController : ControllerBase
{
    private readonly IBenchmarkService _service;

    public BenchmarkController(IBenchmarkService service)
    {
        _service = service;
    }

    /// <summary>
    /// Firmayı benzer firmalarla (en az 5 firmalık grup; önce aynı sektör ve büyüklük) karşılaştırır
    /// ve doğrulanmış Türkiye geneli referansları gösterir. Hiçbir firmanın adı veya tek tek puanı dönmez.
    /// </summary>
    [HttpGet("api/companies/{companyId:guid}/benchmark")]
    public async Task<ActionResult<BenchmarkResponse>> Get(Guid companyId, CancellationToken ct)
    {
        var result = await _service.GetAsync(companyId, ct);
        return result is null ? NotFound() : Ok(result);
    }
}
