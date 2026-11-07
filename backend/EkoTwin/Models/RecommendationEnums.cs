namespace EkoTwin.Models;

/// <summary>Önerinin neye göre tetiklendiği.</summary>
public enum RecommendationTrigger
{
    /// <summary>Tek bir soruya düşük puan (1 veya 2) verildiğinde → somut aksiyon.</summary>
    Question = 1,

    /// <summary>Bir boyutun skoru eşiğin (50) altındaysa → stratejik uyarı.</summary>
    Dimension = 2
}

/// <summary>Üç kademeli düzey: öncelik, maliyet ve etki için ortak kullanılır.</summary>
public enum Level
{
    Low = 1,
    Medium = 2,
    High = 3
}

/// <summary>Önerinin sağladığı etkinin türü (raporda etiket olarak gösterilir).</summary>
public enum ImpactType
{
    /// <summary>Kısa sürede, görünür tasarruf veya iyileşme.</summary>
    QuickWin = 1,

    /// <summary>Mevzuat ve müşteri taleplerine uyum (örn. CBAM).</summary>
    StrategicCompliance = 2,

    /// <summary>Zamanla birikerek artan verimlilik ve değer.</summary>
    LongTermEfficiency = 3
}
