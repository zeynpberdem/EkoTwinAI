namespace EkoTwin.Services;

/// <summary>
/// Türkçe metinler için Ateşman okunabilirlik formülü (Ateşman, E. 1997. "Türkçede okunabilirliğin ölçülmesi",
/// Dil Dergisi, 58, 71-74). Flesch formülünün Türkçeye uyarlanmış hâlidir:
///
///   Skor = 198,825 − 40,175 × (hece / kelime) − 2,610 × (kelime / cümle)
///
///   90-100 çok kolay · 70-89 kolay · 50-69 orta güçlükte · 30-49 zor · 1-29 çok zor
///
/// Türkçede her hece tek bir ünlü içerdiği için hece sayısı = ünlü harf sayısıdır.
/// EkoTwin'de kullanıcıya gösterilen sade metinlerin gerçekten kolay okunduğunu ölçmek için kullanılır.
/// </summary>
public static class Readability
{
    private const string Vowels = "aeıioöuüâîûAEIİOÖUÜÂÎÛ";
    private static readonly char[] SentenceEnds = { '.', '!', '?' };

    public record Result(int Sentences, int Words, int Syllables, decimal Score, string Level);

    public static Result Atesman(string text)
    {
        var words = text
            .Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries)
            .Where(w => w.Any(char.IsLetterOrDigit))
            .ToList();

        var sentences = Math.Max(1, text
            .Split(SentenceEnds, StringSplitOptions.RemoveEmptyEntries)
            .Count(s => s.Any(char.IsLetter)));

        var syllables = words.Sum(w => Math.Max(1, w.Count(c => Vowels.Contains(c))));

        if (words.Count == 0)
            return new Result(0, 0, 0, 0m, Level(0m));

        var score = 198.825m
                    - 40.175m * syllables / words.Count
                    - 2.610m * words.Count / sentences;
        score = Math.Round(score, 1, MidpointRounding.AwayFromZero);

        return new Result(sentences, words.Count, syllables, score, Level(score));
    }

    public static string Level(decimal score) => score switch
    {
        >= 90m => "Çok kolay",
        >= 70m => "Kolay",
        >= 50m => "Orta güçlükte",
        >= 30m => "Zor",
        _ => "Çok zor"
    };
}
