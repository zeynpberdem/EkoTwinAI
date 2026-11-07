using System.ComponentModel.DataAnnotations;
using EkoTwin.Models;
using EkoTwin.Services;

namespace EkoTwin.Dtos;

/// <summary>Firma ekleme ve güncelleme için istemciden gelen veri.</summary>
public class CompanyRequest
{
    [Required(ErrorMessage = "Firma adı zorunludur.")]
    [MaxLength(150, ErrorMessage = "Firma adı en fazla 150 karakter olabilir.")]
    public string CompanyName { get; set; } = string.Empty;

    /// <summary>Standart sektör listesinden bir değer (bkz. GET /api/lookups/sectors).</summary>
    [Required(ErrorMessage = "Sektör seçilmelidir.")]
    [EnumDataType(typeof(SectorType), ErrorMessage = "Geçersiz sektör.")]
    public SectorType? Sector { get; set; }

    /// <summary>İsteğe bağlı alt sektör / ayrıntı.</summary>
    [MaxLength(100, ErrorMessage = "Alt sektör en fazla 100 karakter olabilir.")]
    public string? SubSector { get; set; }

    /// <summary>KOBİ tanımı gereği 250'den az çalışan.</summary>
    [Range(1, 249, ErrorMessage = "Çalışan sayısı 1 ile 249 arasında olmalıdır (KOBİ tanımı).")]
    public int EmployeeCount { get; set; }

    /// <summary>AB ülkelerine ihracat yapılıyor mu?</summary>
    public bool ExportsToEu { get; set; }

    /// <summary>AB'ye satılan ana ürünün CBAM grubu; kapsam dışıysa "None".</summary>
    [EnumDataType(typeof(CbamProductGroup), ErrorMessage = "Geçersiz CBAM ürün grubu.")]
    public CbamProductGroup CbamProductGroup { get; set; } = CbamProductGroup.None;
}

/// <summary>API'nin döndürdüğü firma bilgisi (Türkçe adlarla birlikte).</summary>
public record CompanyResponse(
    Guid Id,
    string CompanyName,
    SectorType Sector,
    string SectorName,
    string SubSector,
    int EmployeeCount,
    bool ExportsToEu,
    CbamProductGroup CbamProductGroup,
    string CbamProductGroupName,
    DateTime CreatedAt)
{
    public static CompanyResponse From(Company c) => new(
        c.Id,
        c.CompanyName,
        c.Sector,
        Lookups.SectorName(c.Sector),
        c.SubSector,
        c.EmployeeCount,
        c.ExportsToEu,
        c.CbamProductGroup,
        Lookups.CbamProductGroupName(c.CbamProductGroup),
        c.CreatedAt);
}

/// <summary>Açılır listeler için değer + Türkçe ad.</summary>
public record LookupItem(string Value, string Name);
