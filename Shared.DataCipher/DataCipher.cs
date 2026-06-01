using System.Security.Cryptography;
using System.Text;

namespace DataCipher;

public static class Encryption
{
    public static string Decrypt(string data)
    {
        string encryptionKey = Environment.GetEnvironmentVariable("EncryptionKey") ??
            throw new Exception("Environment variable EncryptionKey not set");

        byte[] fullCipher = Convert.FromBase64String(data);

        // CMH : 08/23/2025 : Extract salt (first 16 bytes)
        byte[] salt = new byte[16];
        Buffer.BlockCopy(fullCipher, 0, salt, 0, salt.Length);

        // CMH : 08/23/2025 : Extract ciphertext
        byte[] cipherBytes = new byte[fullCipher.Length - salt.Length];
        Buffer.BlockCopy(fullCipher, salt.Length, cipherBytes, 0, cipherBytes.Length);

        // CMH : 08/23/2025 : Derive key & IV again
        using var keyDeriver = new Rfc2898DeriveBytes(
            password: encryptionKey,
            salt: salt,
            iterations: 100_000,
            hashAlgorithm: HashAlgorithmName.SHA256
        );

        byte[] key = keyDeriver.GetBytes(32);
        byte[] iv = keyDeriver.GetBytes(16);

        using var aes = Aes.Create();
        aes.Key = key;
        aes.IV = iv;

        using var ms = new MemoryStream();
        using (var cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Write))
        {
            cs.Write(cipherBytes, 0, cipherBytes.Length);
        }

        return Encoding.UTF8.GetString(ms.ToArray());
    }


    public static string Encrypt(string data)
    {
        string encryptionKey = Environment.GetEnvironmentVariable("EncryptionKey")
        ?? throw new Exception("Environment variable EncryptionKey not set");

        // CMH : 08/23/2025 : Generate a random salt
        byte[] salt = RandomNumberGenerator.GetBytes(16);

        // CMH : 08/23/2025 Derive key & IV from password + salt
        using var keyDeriver = new Rfc2898DeriveBytes(
            password: encryptionKey,
            salt: salt,
            iterations: 100_000,
            hashAlgorithm: HashAlgorithmName.SHA256
        );

        byte[] key = keyDeriver.GetBytes(32); // 256-bit key
        byte[] iv = keyDeriver.GetBytes(16);  // 128-bit IV

        using var aes = Aes.Create();
        aes.Key = key;
        aes.IV = iv;

        using var ms = new MemoryStream();
        using (var cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
        {
            byte[] plainBytes = Encoding.UTF8.GetBytes(data);
            cs.Write(plainBytes, 0, plainBytes.Length);
        }

        byte[] cipherBytes = ms.ToArray();

        // CMH : 08/23/2025 : Combine: SALT + CIPHERTEXT
        byte[] result = new byte[salt.Length + cipherBytes.Length];
        Buffer.BlockCopy(salt, 0, result, 0, salt.Length);
        Buffer.BlockCopy(cipherBytes, 0, result, salt.Length, cipherBytes.Length);

        return Convert.ToBase64String(result);
    }

}
