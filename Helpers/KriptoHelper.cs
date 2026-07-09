using System.Security.Cryptography;
using System.Text;

namespace BankCoreApi.Helpers;

public static class KriptoHelper
{
    // 32-byte AES-256 key (demo; production'da secret store kullanilmali)
    private static readonly byte[] Key = Encoding.UTF8.GetBytes("BankaCuzdanAes256Key!!Secure_32b"); // 32 byte
    private static readonly byte[] Iv = Encoding.UTF8.GetBytes("BankaCuzdanIV16!"); // 16 byte

    public static string Sifrele(string acikMetin)
    {
        if (string.IsNullOrEmpty(acikMetin))
        {
            return string.Empty;
        }

        using var aes = Aes.Create();
        aes.Key = Key;
        aes.IV = Iv;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using var encryptor = aes.CreateEncryptor();
        var plainBytes = Encoding.UTF8.GetBytes(acikMetin);
        var cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
        return Convert.ToBase64String(cipherBytes);
    }

    public static string Coz(string sifreliMetin)
    {
        if (string.IsNullOrEmpty(sifreliMetin))
        {
            return string.Empty;
        }

        using var aes = Aes.Create();
        aes.Key = Key;
        aes.IV = Iv;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using var decryptor = aes.CreateDecryptor();
        var cipherBytes = Convert.FromBase64String(sifreliMetin);
        var plainBytes = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);
        return Encoding.UTF8.GetString(plainBytes);
    }
}
