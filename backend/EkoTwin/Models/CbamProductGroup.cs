namespace EkoTwin.Models;

/// <summary>
/// AB Sınırda Karbon Düzenleme Mekanizması (SKDM / CBAM) kapsamındaki ürün grupları.
/// Kaynak: Tüzük (AB) 2023/956, Ek I (Tüzük (AB) 2025/2083 ile değişik).
/// Firmanın AB'ye sattığı ana ürün grubu seçilir; kapsam dışındaysa <see cref="None"/>.
/// Veritabanında okunabilir olması için metin olarak saklanır.
/// </summary>
public enum CbamProductGroup
{
    /// <summary>CBAM kapsamında değil (mobilya, tekstil, gıda vb.)</summary>
    None = 0,

    /// <summary>Demir ve çelik (döküm, profil, boru, bağlantı elemanı vb.)</summary>
    IronAndSteel = 1,

    /// <summary>Alüminyum</summary>
    Aluminium = 2,

    /// <summary>Çimento</summary>
    Cement = 3,

    /// <summary>Gübre</summary>
    Fertilisers = 4,

    /// <summary>Elektrik</summary>
    Electricity = 5,

    /// <summary>Hidrojen</summary>
    Hydrogen = 6
}
