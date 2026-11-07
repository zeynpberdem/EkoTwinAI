using EkoTwin.Dtos;
using EkoTwin.Models;
using EkoTwin.Services;
using Microsoft.AspNetCore.Mvc;

namespace EkoTwin.Controllers;

/// <summary>
/// Arayüzdeki açılır listeler için sabit değerler ve Türkçe adları.
/// Frontend bu listeleri kendi içinde tekrar tanımlamaz; buradan okur.
/// </summary>
[ApiController]
[Route("api/lookups")]
public class LookupsController : ControllerBase
{
    /// <summary>Standart sektör listesi.</summary>
    [HttpGet("sectors")]
    public ActionResult<IEnumerable<LookupItem>> Sectors() =>
        Ok(Enum.GetValues<SectorType>().Select(s => new LookupItem(s.ToString(), Lookups.SectorName(s))));

    /// <summary>CBAM ürün grupları ("None" = kapsam dışı).</summary>
    [HttpGet("cbam-product-groups")]
    public ActionResult<IEnumerable<LookupItem>> CbamProductGroups() =>
        Ok(Enum.GetValues<CbamProductGroup>().Select(g => new LookupItem(g.ToString(), Lookups.CbamProductGroupName(g))));

    /// <summary>Değerlendirmenin 4 boyutu.</summary>
    [HttpGet("dimensions")]
    public ActionResult<IEnumerable<LookupItem>> Dimensions() =>
        Ok(Enum.GetValues<Dimension>().Select(d => new LookupItem(d.ToString(), Lookups.DimensionName(d))));
}
