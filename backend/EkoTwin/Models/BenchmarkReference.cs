using System.ComponentModel.DataAnnotations;

namespace EkoTwin.Models;

/// <summary>
/// Türkiye geneli resmi referans değeri (örn. TÜİK: "10-49 çalışanlı firmaların %23,6'sı ERP kullanıyor").
/// Sektör karşılaştırmasında ayrı bir kutuda, kaynağıyla gösterilir; EkoTwin puanıyla karıştırılmaz.
///
/// Yalnızca IsVerified = true olan değerler kullanıcıya gösterilir. Değer resmi tablodan
/// teyit edilmeden true yapılmaz.
/// </summary>
public class BenchmarkReference
{
    [Key]
    public Guid Id { get; set; }

    /// <summary>Hangi büyüklük sınıfı için; null ise "10 ve üzeri çalışanı olan tüm firmalar".</summary>
    public SizeClass? SizeClass { get; set; }

    /// <summary>Teknik gösterge adı (örn. "ERP yazılımı kullanan girişim oranı").</summary>
    [Required]
    [MaxLength(200)]
    public string Indicator { get; set; } = string.Empty;

    /// <summary>Sade cümle parçası (örn. "işlerini birbirine bağlı tek bir programda yürütüyor").</summary>
    [Required]
    [MaxLength(200)]
    public string SimpleIndicator { get; set; } = string.Empty;

    /// <summary>Oran (%).</summary>
    public decimal Percent { get; set; }

    /// <summary>Verinin yılı.</summary>
    public int Year { get; set; }

    /// <summary>
    /// Firmanın cevabıyla karşılaştırma: bu soruda bu puan ve üstünü seçen firma göstergeye "dahil" sayılır.
    /// Null ise yalnızca bilgi olarak gösterilir (firmanın cevabıyla eşleşmeyen göstergeler).
    /// </summary>
    public int? QuestionNumber { get; set; }
    public int? MinAnswerScore { get; set; }

    /// <summary>Kaynak (kurum, araştırma adı, yıl).</summary>
    [Required]
    [MaxLength(300)]
    public string Source { get; set; } = string.Empty;

    /// <summary>Resmi tablodan teyit edildi mi? Edilmediyse kullanıcıya gösterilmez.</summary>
    public bool IsVerified { get; set; }
}
