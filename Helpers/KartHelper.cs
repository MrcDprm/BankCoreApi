namespace BankCoreApi.Helpers;

public record KartUretimSonucu(
    string KartNo,
    string Cvv,
    int SonKullanmaAy,
    int SonKullanmaYil);

public static class KartHelper
{
    public static KartUretimSonucu Uret()
    {
        var kartNo = UretLuhnKartNo();
        var cvv = Random.Shared.Next(0, 1000).ToString("D3");
        var skt = DateTime.UtcNow.AddYears(3);

        return new KartUretimSonucu(kartNo, cvv, skt.Month, skt.Year);
    }

    private static string UretLuhnKartNo()
    {
        // Mastercard: 16 hane, 5 ile baslar
        Span<int> digits = stackalloc int[16];
        digits[0] = 5;

        for (var i = 1; i < 15; i++)
        {
            digits[i] = Random.Shared.Next(0, 10);
        }

        digits[15] = LuhnCheckDigit(digits[..15]);

        return string.Concat(digits.ToArray().Select(d => d.ToString()));
    }

    private static int LuhnCheckDigit(ReadOnlySpan<int> payload)
    {
        var sum = 0;
        var doubleDigit = true;

        for (var i = payload.Length - 1; i >= 0; i--)
        {
            var d = payload[i];
            if (doubleDigit)
            {
                d *= 2;
                if (d > 9)
                {
                    d -= 9;
                }
            }

            sum += d;
            doubleDigit = !doubleDigit;
        }

        return (10 - (sum % 10)) % 10;
    }
}
