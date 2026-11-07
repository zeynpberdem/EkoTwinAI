using System.Globalization;

namespace EkoTwin.Services;

/// <summary>Rapor metinlerindeki sayıların Türkçe biçimi (ondalık ayırıcı virgül): 18,75 · +6,25 · %10,9</summary>
public static class TurkishFormat
{
    public static readonly CultureInfo Culture = CultureInfo.GetCultureInfo("tr-TR");

    /// <summary>Örn. 18.75 → "18,75", 25 → "25".</summary>
    public static string Number(decimal value, string format = "0.##") => value.ToString(format, Culture);

    /// <summary>İşaretli: 6.25 → "+6,25", -6.25 → "-6,25", 0 → "0".</summary>
    public static string Signed(decimal value, string format = "0.##") =>
        value > 0 ? "+" + Number(value, format) : Number(value, format);
}
