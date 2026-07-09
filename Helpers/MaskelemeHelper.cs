namespace BankCoreApi.Helpers;

public static class MaskelemeHelper
{
    public static string MaskeleAdSoyad(string? tamAd)
    {
        if (string.IsNullOrWhiteSpace(tamAd))
        {
            return string.Empty;
        }

        var kelimeler = tamAd
            .Trim()
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var maskelenmisKelimeler = kelimeler.Select(MaskeleKelime);
        return string.Join(' ', maskelenmisKelimeler);
    }

    private static string MaskeleKelime(string kelime)
    {
        if (string.IsNullOrEmpty(kelime))
        {
            return string.Empty;
        }

        if (kelime.Length <= 2)
        {
            return kelime;
        }

        var gorunen = kelime[..2];
        var yildizSayisi = kelime.Length - 2;
        return gorunen + new string('*', yildizSayisi);
    }
}
