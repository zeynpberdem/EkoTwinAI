using System.Globalization;
using EkoTwin.Data;
using EkoTwin.Dtos;
using EkoTwin.Models;
using EkoTwin.Services;
using Row = EkoTwin.Services.BenchmarkCalculator.ScoreRow;
using CRow = EkoTwin.Services.BenchmarkCalculator.CarbonRow;

namespace EkoTwin.Tests;

/// <summary>Sektör karşılaştırmasının kuralları: grup seçimi, ortalama/sıralama, sade metinler, resmi referanslar.</summary>
public class BenchmarkCalculatorTests
{
    private static readonly Guid Me = Guid.NewGuid();

    private static Row R(SectorType sector, SizeClass size, decimal total, Guid? id = null) =>
        new(id ?? Guid.NewGuid(), sector, size, total, total, total, total, total);

    private static (BenchmarkGroupLevel Level, List<Row> Peers)? Select(IEnumerable<Row> rows) =>
        BenchmarkCalculator.SelectGroup(Me, SectorType.FurnitureAndWood, SizeClass.Small, rows,
            r => r.CompanyId, r => r.Sector, r => r.Size);

    // ===================== Büyüklük sınıfı =====================

    [Theory]
    [InlineData(1, SizeClass.Micro)]
    [InlineData(9, SizeClass.Micro)]
    [InlineData(10, SizeClass.Small)]
    [InlineData(49, SizeClass.Small)]
    [InlineData(50, SizeClass.Medium)]
    [InlineData(249, SizeClass.Medium)]
    public void BuyuklukSinifi_KobiTanimina_Gore(int employees, SizeClass expected) =>
        Assert.Equal(expected, BenchmarkCalculator.SizeOf(employees));

    // ===================== Grup seçimi =====================

    [Fact]
    public void AyniSektorVeBuyuklukte5Firma_EnDarGrupSecilir()
    {
        var rows = Enumerable.Range(0, 5).Select(_ => R(SectorType.FurnitureAndWood, SizeClass.Small, 50)).ToList();

        Assert.Equal(BenchmarkGroupLevel.SectorAndSize, Select(rows)!.Value.Level);
    }

    [Fact]
    public void AyniBuyuklukteYetersiz_AyniSektorYeterli_SektorGrubu()
    {
        var rows = Enumerable.Range(0, 4).Select(_ => R(SectorType.FurnitureAndWood, SizeClass.Small, 50))
            .Concat(new[] { R(SectorType.FurnitureAndWood, SizeClass.Medium, 50), R(SectorType.FurnitureAndWood, SizeClass.Micro, 50) })
            .ToList();

        var g = Select(rows)!.Value;
        Assert.Equal(BenchmarkGroupLevel.Sector, g.Level);
        Assert.Equal(6, g.Peers.Count);
    }

    [Fact]
    public void SektorYetersiz_BuyuklukYeterli_BuyuklukGrubu()
    {
        var rows = Enumerable.Range(0, 5).Select(_ => R(SectorType.FoodAndBeverage, SizeClass.Small, 50)).ToList();

        Assert.Equal(BenchmarkGroupLevel.Size, Select(rows)!.Value.Level);
    }

    [Fact]
    public void YalnizcaToplamda5Firma_TumFirmalar()
    {
        var rows = Enumerable.Range(0, 5).Select(_ => R(SectorType.FoodAndBeverage, SizeClass.Medium, 50)).ToList();

        Assert.Equal(BenchmarkGroupLevel.All, Select(rows)!.Value.Level);
    }

    [Fact]
    public void FirmaninKendisiSayilmaz_DortFirmaylaKarsilastirmaYok()
    {
        var rows = Enumerable.Range(0, 4).Select(_ => R(SectorType.FurnitureAndWood, SizeClass.Small, 50))
            .Append(R(SectorType.FurnitureAndWood, SizeClass.Small, 90, Me))
            .ToList();

        Assert.Null(Select(rows));
    }

    [Fact]
    public void GrupAciklamasi_SadeDil()
    {
        Assert.Equal("Mobilya ve Ağaç Ürünleri sektöründeki 12 firmayla karşılaştırıldınız.",
            BenchmarkCalculator.GroupDescription(BenchmarkGroupLevel.Sector, SectorType.FurnitureAndWood, SizeClass.Small, 12));
        Assert.Equal("Sizin büyüklüğünüzdeki (10-49 çalışan) 7 firmayla karşılaştırıldınız.",
            BenchmarkCalculator.GroupDescription(BenchmarkGroupLevel.Size, SectorType.FurnitureAndWood, SizeClass.Small, 7));
    }

    // ===================== Puan karşılaştırması =====================

    [Fact]
    public void Ortalama_DahaIyiOlunanOran_VeSadeMetin()
    {
        var peers = new[] { 40m, 50m, 60m, 70m, 80m }.Select(t => R(SectorType.FurnitureAndWood, SizeClass.Small, t)).ToList();
        var total = BenchmarkCalculator.CompareScores(R(SectorType.FurnitureAndWood, SizeClass.Small, 60, Me), peers)[0];

        Assert.Null(total.Dimension);
        Assert.Equal(60m, total.GroupAverage);
        Assert.Equal(40.0m, total.BetterThanPercent); // 40 ve 50 → 2/5 (eşit olan sayılmaz)
        Assert.Equal(4, total.BetterThanOutOf10);
        Assert.Equal(BenchmarkPosition.Average, total.Position);
        Assert.Equal("Genel puanda puanınız 60, ortalama 60. Ortalama düzeydesiniz. 10 firmadan 4'ünden daha iyisiniz.", total.SimpleText);
    }

    [Theory]
    [InlineData(65, BenchmarkPosition.Above)]    // ortalama 60, fark +5
    [InlineData(64, BenchmarkPosition.Average)]
    [InlineData(55, BenchmarkPosition.Below)]    // fark −5
    public void Konum_5PuanEsigi(decimal mine, BenchmarkPosition expected)
    {
        var peers = Enumerable.Repeat(60m, 5).Select(t => R(SectorType.FurnitureAndWood, SizeClass.Small, t)).ToList();
        var total = BenchmarkCalculator.CompareScores(R(SectorType.FurnitureAndWood, SizeClass.Small, mine, Me), peers)[0];

        Assert.Equal(expected, total.Position);
    }

    [Fact]
    public void BesMetrik_GenelVeDortBoyut_SadeAdlarla()
    {
        var peers = Enumerable.Repeat(50m, 5).Select(t => R(SectorType.FurnitureAndWood, SizeClass.Small, t)).ToList();
        var metrics = BenchmarkCalculator.CompareScores(R(SectorType.FurnitureAndWood, SizeClass.Small, 75, Me), peers);

        Assert.Equal(new[] { "Genel puan", "Dijital düzen", "Enerji tasarrufu", "Karbon takibi", "Tedarikçiler ve malzeme" },
            metrics.Select(m => m.SimpleName));
        Assert.StartsWith("Enerji tasarrufu konusunda puanınız 75, ortalama 50. Ortalamanın üstündesiniz. 10 firmadan 10'undan", metrics[2].SimpleText);
    }

    // ===================== Karbon karşılaştırması =====================

    [Fact]
    public void Karbon_DusukOlanIyi()
    {
        var peers = new[] { 2m, 4m, 6m, 8m, 10m }.Select(t => new CRow(Guid.NewGuid(), SectorType.FurnitureAndWood, SizeClass.Small, 2025, t)).ToList();
        var group = new BenchmarkGroupResponse(BenchmarkGroupLevel.SectorAndSize, 5, "x");
        var r = BenchmarkCalculator.CompareCarbon(new CRow(Me, SectorType.FurnitureAndWood, SizeClass.Small, 2025, 5m), group, peers);

        Assert.Equal(6m, r.GroupAverageTonnesPerEmployee);
        Assert.Equal(60.0m, r.LowerThanPercent);   // 6, 8, 10 → 3/5
        Assert.Equal(6, r.LowerThanOutOf10);
        Assert.Equal(BenchmarkPosition.Above, r.Position); // ortalamadan %16,7 düşük
        Assert.EndsWith("10 firmadan 6'sından daha az karbon salıyorsunuz.", r.SimpleText);
    }

    // ===================== Türkçe ekler =====================

    [Theory]
    [InlineData(1, "1'inden")]
    [InlineData(3, "3'ünden")]
    [InlineData(6, "6'sından")]
    [InlineData(9, "9'undan")]
    [InlineData(10, "10'undan")]
    public void OnFirmadanKacindan_EkDogru(int n, string expected) =>
        Assert.Equal(expected, BenchmarkCalculator.WithAblative(n));

    [Fact]
    public void HicbirFirmadanIyiDegilse_AnlasilirCumle() =>
        Assert.Equal("Şu an benzer firmaların çoğunun gerisindesiniz.", BenchmarkCalculator.BetterThanText(0));

    [Theory]
    [InlineData(23.6, "yüzde 24'ü")]
    [InlineData(46, "yüzde 46'sı")]
    [InlineData(16.7, "yüzde 17'si")]
    [InlineData(83, "yüzde 83'ü")]
    [InlineData(33.3, "yüzde 33'ü")]
    [InlineData(30, "yüzde 30'u")]
    [InlineData(40, "yüzde 40'ı")]
    [InlineData(50, "yüzde 50'si")]
    [InlineData(100, "yüzde 100'ü")]
    public void YuzdeIyelikEki_SayininOkunusunaGore(double percent, string expected) =>
        Assert.Equal(expected, PlainLanguage.PercentPossessive((decimal)percent));

    // ===================== Türkiye geneli referanslar =====================

    private static BenchmarkReference Erp(bool verified) => new()
    {
        SizeClass = SizeClass.Small, Indicator = "ERP", SimpleIndicator = "işlerini birbirine bağlı tek bir programda yürütüyor",
        Percent = 23.6m, Year = 2025, QuestionNumber = 1, MinAnswerScore = 4, Source = "TÜİK", IsVerified = verified
    };

    [Fact]
    public void DogrulanmamisReferans_Gosterilmez()
    {
        var refs = BenchmarkCalculator.NationalReferences(SizeClass.Small, new[] { Erp(false) }, new Dictionary<int, int> { [1] = 5 });

        Assert.Empty(refs);
    }

    [Fact]
    public void MikroFirmaya_TuikReferansiGosterilmez()
    {
        var refs = BenchmarkCalculator.NationalReferences(SizeClass.Micro, new[] { Erp(true) }, new Dictionary<int, int> { [1] = 5 });

        Assert.Empty(refs);
    }

    [Theory]
    [InlineData(4, true, "Siz de bunlardan birisiniz.")]
    [InlineData(3, false, "Siz henüz bunlardan biri değilsiniz.")]
    public void DogrulanmisReferans_CevabaGoreDurum(int answer, bool included, string status)
    {
        var r = BenchmarkCalculator.NationalReferences(SizeClass.Small, new[] { Erp(true) }, new Dictionary<int, int> { [1] = answer }).Single();

        Assert.Equal("Türkiye'de sizin büyüklüğünüzdeki (10-49 çalışan) firmaların yüzde 24'ü işlerini birbirine bağlı tek bir programda yürütüyor.", r.SimpleText);
        Assert.Equal(included, r.YouAreIncluded);
        Assert.Equal(status, r.SimpleYourStatus);
    }

    [Fact]
    public void BaskaBuyuklugunReferansi_Gosterilmez()
    {
        var refs = BenchmarkCalculator.NationalReferences(SizeClass.Medium, new[] { Erp(true) }, new Dictionary<int, int>());

        Assert.Empty(refs);
    }

    // ===================== Seed bütünlüğü =====================

    [Fact]
    public void Seed_KaynakliTutarliVeSadeDilde()
    {
        var tr = CultureInfo.GetCultureInfo("tr-TR");
        foreach (var r in BenchmarkReferenceSeed.References)
        {
            Assert.Contains("TÜİK", r.Source);
            Assert.InRange(r.Percent, 0m, 100m);
            Assert.Equal(r.QuestionNumber is null, r.MinAnswerScore is null);
            Assert.NotEqual(SizeClass.Micro, r.SizeClass);
            Assert.Contains("veriportali.tuik.gov.tr/tr/press/", r.Source); // resmi bülten bağlantısı
            Assert.True(r.IsVerified, $"{r.Indicator} ({r.SizeClass}) doğrulanmamış");
            Assert.DoesNotContain(QuestionSeedTests.Jargon, term => tr.CompareInfo.IndexOf(r.SimpleIndicator, term, CompareOptions.IgnoreCase) >= 0);
        }
    }
}
