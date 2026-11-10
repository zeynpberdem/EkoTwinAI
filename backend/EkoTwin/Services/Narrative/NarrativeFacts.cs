using EkoTwin.Dtos;

namespace EkoTwin.Services.Narrative;

/// <summary>
/// Mektubun ham maddesi: kuralların (A1-A6) ürettiği sade ve doğruluğu garanti bilgiler.
/// Hem yapay zekâya "bilgi kartı" olarak verilir hem de kural tabanlı şablonu besler.
/// Firma adı burada YOKTUR (gizlilik).
/// </summary>
public record NarrativeInput(
    AssessmentResponse Report,
    CarbonFootprintResponse? Carbon,
    ProgressResponse? Progress,
    BenchmarkResponse? Benchmark);

public static class NarrativeFacts
{
    public static IReadOnlyList<string> Build(NarrativeInput input)
    {
        var r = input.Report;
        var facts = new List<string>
        {
            $"Genel puan 100 üzerinden {PlainLanguage.Points(r.TotalScore)}, seviye: {r.SimpleMaturityLevel}."
        };

        foreach (var d in r.DimensionScores)
            facts.Add($"{d.SimpleName}: 100 üzerinden {PlainLanguage.Points(d.Score)} puan ({d.SimpleMaturityLevel}).");

        var strongest = r.DimensionScores.OrderByDescending(d => d.Score).First();
        facts.Add($"En güçlü olduğu konu: {strongest.SimpleName}.");

        if (r.Recommendations.BottleneckDimension is { } bottleneck)
            facts.Add($"En çok zorlandığı konu: {ScoreCalculator.SimpleName(bottleneck)}.");

        var i = 1;
        foreach (var a in r.Recommendations.TopActions)
        {
            facts.Add($"En acil adım {i}: {a.SimpleTitle}. Ne kazandırır: {a.SimpleBenefit} " +
                      $"({a.SimpleCostText}, yaklaşık {a.DurationText}).");
            i++;
        }

        if (r.CbamRisk.IsInScope)
            facts.Add($"Avrupa karbon kuralı: {r.CbamRisk.SimpleRiskLevel}. {r.CbamRisk.SimpleSummary}");

        if (input.Carbon is { } c)
        {
            facts.Add(c.Simple.Headline + ".");
            if (c.Simple.LargestSource is { } largest) facts.Add(largest);
            if (c.Simple.Comparison is { } comparison) facts.Add(comparison);
        }

        // Gelişim yalnızca bu değerlendirme en son karşılaştırmanın "şimdiki" testi ise anlatılır
        if (input.Progress?.LatestComparison is { } cmp && cmp.Current.Id == r.Id)
        {
            facts.Add(cmp.TotalChange > 0
                ? $"Önceki teste göre genel puan yükseldi: {PlainLanguage.Points(cmp.Previous.TotalScore)} → {PlainLanguage.Points(cmp.Current.TotalScore)}."
                : cmp.TotalChange < 0
                    ? $"Önceki teste göre genel puan düştü: {PlainLanguage.Points(cmp.Previous.TotalScore)} → {PlainLanguage.Points(cmp.Current.TotalScore)}."
                    : "Önceki teste göre genel puan değişmedi.");
            facts.Add(cmp.RecommendationFollowUp.Summary.SimpleSummary);
        }

        if (input.Benchmark?.Group is { } group && input.Benchmark.Metrics.Count > 0)
        {
            facts.Add(group.SimpleDescription);
            facts.Add(input.Benchmark.Metrics[0].SimpleText);
        }

        return facts;
    }
}

/// <summary>
/// Kural tabanlı mektup: yapay zekâ ayarlı değilse, yanıt vermezse veya metni kontrolden geçemezse kullanılır.
/// Yalnızca bilgi kartındaki cümlelerden oluşur; her zaman doğrudur.
/// </summary>
public static class NarrativeTemplate
{
    public const string Closing =
        "Bu rapor, verdiğiniz cevaplara dayanan bir ön değerlendirmedir. Adımları atarken bir uzmana danışmanızı öneririz. " +
        "Testi yaklaşık 6 ay sonra yeniden doldurarak ilerlemenizi görebilirsiniz.";

    public static string Build(NarrativeInput input)
    {
        var r = input.Report;
        var paragraphs = new List<string>();

        var strongest = r.DimensionScores.OrderByDescending(d => d.Score).First();
        var first = $"Genel puanınız 100 üzerinden {PlainLanguage.Points(r.TotalScore)}. Bu sonuçla \"{r.SimpleMaturityLevel}\" seviyesindesiniz. " +
                    $"En güçlü olduğunuz konu {strongest.SimpleName.ToLower(TurkishFormat.Culture)}.";
        if (r.Recommendations.BottleneckDimension is { } b && b != strongest.Dimension)
            first += $" En çok zorlandığınız konu ise {ScoreCalculator.SimpleName(b).ToLower(TurkishFormat.Culture)}.";
        paragraphs.Add(first);

        if (r.Recommendations.TopActions.Count > 0)
        {
            var lines = r.Recommendations.TopActions.Select((a, idx) =>
                $"{idx + 1}. {a.SimpleTitle}: {a.SimpleBenefit} ({a.SimpleCostText}, yaklaşık {a.DurationText}).");
            paragraphs.Add("Şimdi atmanızı önerdiğimiz en acil adımlar şunlar:\n" + string.Join("\n", lines));
        }

        if (r.CbamRisk.IsInScope)
            paragraphs.Add(r.CbamRisk.SimpleSummary);

        if (input.Carbon is { } c)
        {
            var carbon = c.Simple.Headline + ".";
            if (c.Simple.LargestSource is { } largest) carbon += " " + largest;
            if (c.Simple.Comparison is { } comparison) carbon += " " + comparison;
            paragraphs.Add(carbon);
        }

        if (input.Progress?.LatestComparison is { } cmp && cmp.Current.Id == r.Id)
            paragraphs.Add(cmp.RecommendationFollowUp.Summary.SimpleSummary);

        if (input.Benchmark?.Group is { } group && input.Benchmark.Metrics.Count > 0)
            paragraphs.Add(group.SimpleDescription + " " + input.Benchmark.Metrics[0].SimpleText);

        paragraphs.Add(Closing);
        return string.Join("\n\n", paragraphs);
    }
}
