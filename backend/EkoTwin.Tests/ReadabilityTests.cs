using EkoTwin.Services;

namespace EkoTwin.Tests;

/// <summary>Ateşman okunabilirlik formülünün doğru hesaplandığı.</summary>
public class ReadabilityTests
{
    [Fact]
    public void ElleHesapla_Tutar()
    {
        // "Ali eve geldi." → 3 kelime, 6 hece (A-li, e-ve, gel-di), 1 cümle
        // 198,825 − 40,175 × 2 − 2,610 × 3 = 110,645 → 110,6
        var r = Readability.Atesman("Ali eve geldi.");

        Assert.Equal((1, 3, 6), (r.Sentences, r.Words, r.Syllables));
        Assert.Equal(110.6m, r.Score);
        Assert.Equal("Çok kolay", r.Level);
    }

    [Fact]
    public void CumleSayisi_NoktaSoruVeUnlemeGore()
    {
        var r = Readability.Atesman("Geldi mi? Evet geldi! Çok iyi.");
        Assert.Equal(3, r.Sentences);
    }

    [Fact]
    public void UzunKelimeVeUzunCumle_SkoruDusurur()
    {
        var easy = Readability.Atesman("Faturayı ödedik. Sayaç taktık.");
        var hard = Readability.Atesman("Entegrasyon kapsamında optimizasyon çalışmalarının sürdürülebilirliğinin değerlendirilmesi gerçekleştirilmiştir.");

        Assert.True(easy.Score > hard.Score);
        Assert.Equal("Çok zor", hard.Level);
    }

    [Fact]
    public void BosMetin_Sifir()
    {
        Assert.Equal(0m, Readability.Atesman("   ").Score);
    }

    [Theory]
    [InlineData(95, "Çok kolay")]
    [InlineData(70, "Kolay")]
    [InlineData(55, "Orta güçlükte")]
    [InlineData(30, "Zor")]
    [InlineData(10, "Çok zor")]
    public void Seviyeler(decimal score, string level) => Assert.Equal(level, Readability.Level(score));
}
