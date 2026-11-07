using EkoTwin.Models;
using EkoTwin.Services;

namespace EkoTwin.Tests;

/// <summary>
/// CBAM risk değerlendirmesinin testleri.
/// Kural: kapsam dışı ürün → "Kapsam dışı"; kapsamda ama AB'ye ihracat yok → "Düşük";
/// kapsamda + AB'ye ihracat → Karbon skoru &lt; 40 Yüksek, &lt; 70 Orta, ≥ 70 Düşük.
/// </summary>
public class CbamRiskEvaluatorTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void KapsamDisiUrun_IhracattanBagimsizKapsamDisidir(bool exportsToEu)
    {
        var risk = CbamRiskEvaluator.Evaluate(exportsToEu, CbamProductGroup.None, carbonScore: 0m);

        Assert.False(risk.IsInScope);
        Assert.Equal(CbamRiskEvaluator.OutOfScope, risk.RiskLevel);
    }

    [Fact]
    public void KapsamdakiUrun_AbyeIhracatYoksa_DusukRisk()
    {
        var risk = CbamRiskEvaluator.Evaluate(exportsToEu: false, CbamProductGroup.IronAndSteel, carbonScore: 0m);

        Assert.True(risk.IsInScope);
        Assert.Equal(CbamRiskEvaluator.Low, risk.RiskLevel);
    }

    [Theory]
    [InlineData(0, "Yüksek")]
    [InlineData(39.99, "Yüksek")]
    [InlineData(40, "Orta")]      // sınır değer
    [InlineData(69.99, "Orta")]
    [InlineData(70, "Düşük")]     // sınır değer
    [InlineData(100, "Düşük")]
    public void KapsamdakiUrun_AbyeIhracatVarsa_RiskKarbonSkorunaGoreBelirlenir(double carbonScore, string expected)
    {
        var risk = CbamRiskEvaluator.Evaluate(exportsToEu: true, CbamProductGroup.IronAndSteel, (decimal)carbonScore);

        Assert.True(risk.IsInScope);
        Assert.Equal(expected, risk.RiskLevel);
    }

    [Fact]
    public void YuksekVeOrtaRiskte_DogrulamaOnerisiVerilir_DusukteVerilmez()
    {
        const string verification = "doğrulayıcı";

        var high = CbamRiskEvaluator.Evaluate(true, CbamProductGroup.Aluminium, 10m);
        var medium = CbamRiskEvaluator.Evaluate(true, CbamProductGroup.Aluminium, 50m);
        var low = CbamRiskEvaluator.Evaluate(true, CbamProductGroup.Aluminium, 90m);

        Assert.Contains(high.Actions, a => a.Contains(verification));
        Assert.Contains(medium.Actions, a => a.Contains(verification));
        Assert.DoesNotContain(low.Actions, a => a.Contains(verification));
    }

    [Theory]
    [InlineData(CbamProductGroup.Electricity, true)]
    [InlineData(CbamProductGroup.Hydrogen, true)]
    [InlineData(CbamProductGroup.IronAndSteel, false)]
    [InlineData(CbamProductGroup.Cement, false)]
    public void ElektrikVeHidrojende_50TonMuafiyetiOlmadigiBelirtilir(CbamProductGroup group, bool expectsNote)
    {
        var risk = CbamRiskEvaluator.Evaluate(true, group, 50m);

        Assert.Equal(expectsNote, risk.Actions.Any(a => a.Contains("50 tonluk muafiyet")));
    }

    [Fact]
    public void Ozet_UrunGrubununTurkceAdiniVeSkoruIcerir()
    {
        var risk = CbamRiskEvaluator.Evaluate(true, CbamProductGroup.IronAndSteel, 12.5m);

        Assert.Equal("Demir-çelik", risk.ProductGroupName);
        Assert.Contains("Demir-çelik", risk.Summary);
        Assert.Contains("12", risk.Summary);
    }
}
