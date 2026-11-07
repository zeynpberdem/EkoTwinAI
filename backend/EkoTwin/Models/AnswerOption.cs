using System.ComponentModel.DataAnnotations;

namespace EkoTwin.Models;

/// <summary>
/// Bir sorunun cevap seçeneği. Kullanıcı "3" yerine "Bazı işler için program kullanıyoruz" gibi
/// anlamlı bir cümle seçer; arka planda seçeneğin puanı (1-5) saklanır.
/// </summary>
public class AnswerOption
{
    [Key]
    public Guid Id { get; set; }

    public Guid QuestionId { get; set; }
    public Question Question { get; set; } = null!;

    /// <summary>Seçeneğin puanı (1-5). Her soruda her puan için tek seçenek vardır.</summary>
    public int Score { get; set; }

    /// <summary>Kullanıcıya gösterilen seçenek metni.</summary>
    [Required]
    [MaxLength(150)]
    public string Label { get; set; } = string.Empty;
}
