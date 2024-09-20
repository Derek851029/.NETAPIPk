using System.Security.Cryptography;
using System.Text;

namespace PKApp.Services
{
    public interface ICrypto
    {
        Task<string> Encrypt(string str);
        Task<string> Decrypt(string str);
    }

    public class CryptoService : ICrypto
    {
        private readonly IConfiguration _configuration;

        public CryptoService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<string> Encrypt(string str)
        {
            string key = _configuration["GameCryptokey"];
            string iv = _configuration["GameCryptoIv"];
            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = Encoding.UTF8.GetBytes(key);
                aesAlg.IV = Encoding.UTF8.GetBytes(iv);

                ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

                using (MemoryStream msEncrypt = new MemoryStream())
                {
                    using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                        {
                            swEncrypt.Write(str);
                        }
                    }

                    return Convert.ToBase64String(msEncrypt.ToArray());
                }
            }
        }

        public async Task<string> Decrypt(string str)
        {
            string key = _configuration["GameCryptokey"];
            string iv = _configuration["GameCryptoIv"];
            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = Encoding.UTF8.GetBytes(key);
                aesAlg.IV = Encoding.UTF8.GetBytes(iv);

                ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

                using (MemoryStream msDecrypt = new MemoryStream(Convert.FromBase64String(str)))
                {
                    using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                    {
                        using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                        {
                            return srDecrypt.ReadToEnd();
                        }
                    }
                }
            }
        }
    }
}
