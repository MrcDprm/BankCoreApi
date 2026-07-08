using OtpNet;

namespace BankCoreApi.Services;

public class TotpServis : ITotpServis
{
    public string GizliAnahtarUret()
    {
        var anahtarBaytlar = KeyGeneration.GenerateRandomKey(20);
        var secretKey = Base32Encoding.ToString(anahtarBaytlar);
        return secretKey;
    }

    public bool KoduDogrula(string secretKey, string kod)
    {
        var totp = new Totp(Base32Encoding.ToBytes(secretKey));
        return totp.VerifyTotp(kod, out _);
    }
}
