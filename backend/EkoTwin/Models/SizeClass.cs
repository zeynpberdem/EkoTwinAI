namespace EkoTwin.Models;

/// <summary>
/// KOBİ büyüklük sınıfı (çalışan sayısına göre; KOBİ Yönetmeliği'ndeki sınıflandırma).
/// Sektör karşılaştırmasında benzer firmaları gruplamak için kullanılır.
/// </summary>
public enum SizeClass
{
    /// <summary>1-9 çalışan</summary>
    Micro = 1,

    /// <summary>10-49 çalışan</summary>
    Small = 2,

    /// <summary>50-249 çalışan</summary>
    Medium = 3
}
