namespace BankCoreApi.Services;

public interface ITotpServis
{
    string GizliAnahtarUret();
    bool KoduDogrula(string secretKey, string kod);
}
