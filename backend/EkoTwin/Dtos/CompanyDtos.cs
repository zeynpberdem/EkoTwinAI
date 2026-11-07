using System.ComponentModel.DataAnnotations;
using EkoTwin.Models;

namespace EkoTwin.Dtos;

/// <summary>Yeni firma kaydı için istemciden gelen veri.</summary>
public class CreateCompanyRequest
{
    [Required(ErrorMessage = "Firma adı zorunludur.")]
    [MaxLength(150, ErrorMessage = "Firma adı en fazla 150 karakter olabilir.")]
    public string CompanyName { get; set; } = string.Empty;

    [MaxLength(100, ErrorMessage = "Sektör en fazla 100 karakter olabilir.")]
    public string Sector { get; set; } = string.Empty;

    /// <summary>KOBİ tanımı gereği 250'den az çalışan.</summary>
    [Range(1, 249, ErrorMessage = "Çalışan sayısı 1 ile 249 arasında olmalıdır (KOBİ tanımı).")]
    public int EmployeeCount { get; set; }

    /// <summary>AB ülkelerine ihracat yapılıyor mu?</summary>
    public bool ExportsToEu { get; set; }

    /// <summary>AB'ye satılan ana ürünün CBAM grubu; kapsam dışıysa "None".</summary>
    [EnumDataType(typeof(CbamProductGroup), ErrorMessage = "Geçersiz CBAM ürün grubu.")]
    public CbamProductGroup CbamProductGroup { get; set; } = CbamProductGroup.None;
}

/// <summary>API'nin döndürdüğü firma bilgisi.</summary>
public record CompanyResponse(
    Guid Id,
    string CompanyName,
    string Sector,
    int EmployeeCount,
    bool ExportsToEu,
    CbamProductGroup CbamProductGroup,
    DateTime CreatedAt)
{
    public static CompanyResponse From(Company c) =>
        new(c.Id, c.CompanyName, c.Sector, c.EmployeeCount, c.ExportsToEu, c.CbamProductGroup, c.CreatedAt);
}
