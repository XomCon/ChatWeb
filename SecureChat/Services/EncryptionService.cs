using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using SecureChat.Models;

namespace SecureChat.Services
{
    public class EncryptionService
    {
        private readonly byte[] _keyBytes;

        public EncryptionService(IOptions<EncryptionSettings> encryptionSettings)
        {
            var key = encryptionSettings.Value.Key;

            if (string.IsNullOrWhiteSpace(key))
            {
                throw new Exception("Encryption key is missing in appsettings.json");
            }

            var keyRaw = Encoding.UTF8.GetBytes(key);

            _keyBytes = new byte[32];
            Array.Copy(keyRaw, _keyBytes, Math.Min(keyRaw.Length, _keyBytes.Length));
        }

        public string Encrypt(string plainText)
        {
            if (string.IsNullOrWhiteSpace(plainText))
                return string.Empty;

            using var aes = Aes.Create();
            aes.Key = _keyBytes;
            aes.GenerateIV();
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

            var plainBytes = Encoding.UTF8.GetBytes(plainText);
            var cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

            var combined = new byte[aes.IV.Length + cipherBytes.Length];
            Buffer.BlockCopy(aes.IV, 0, combined, 0, aes.IV.Length);
            Buffer.BlockCopy(cipherBytes, 0, combined, aes.IV.Length, cipherBytes.Length);

            return Convert.ToBase64String(combined);
        }

        public string Decrypt(string cipherText)
        {
            if (string.IsNullOrWhiteSpace(cipherText))
                return string.Empty;

            try
            {
                if (!IsProbablyBase64(cipherText))
                {
                    return cipherText;
                }

                var combined = Convert.FromBase64String(cipherText);

                if (combined.Length <= 16)
                {
                    return cipherText;
                }

                using var aes = Aes.Create();
                aes.Key = _keyBytes;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                var iv = new byte[16];
                var cipherBytes = new byte[combined.Length - 16];

                Buffer.BlockCopy(combined, 0, iv, 0, 16);
                Buffer.BlockCopy(combined, 16, cipherBytes, 0, cipherBytes.Length);

                aes.IV = iv;

                using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
                var plainBytes = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);

                return Encoding.UTF8.GetString(plainBytes);
            }
            catch
            {
                return cipherText;
            }
        }

        private bool IsProbablyBase64(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return false;

            value = value.Trim();

            if (value.Length % 4 != 0)
                return false;

            return Convert.TryFromBase64String(value, new Span<byte>(new byte[value.Length]), out _);
        }
    }
}