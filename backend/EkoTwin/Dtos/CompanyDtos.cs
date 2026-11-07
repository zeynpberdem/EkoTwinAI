using System.ComponentModel.DataAnnotations;

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
}

/// <summary>API'nin döndürdüğü firma bilgisi.</summary>
public record CompanyResponse(
    Guid Id,
    string CompanyName,
    string Sector,
    int EmployeeCount,
    DateTime CreatedAt);
