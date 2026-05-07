using System.Security.Cryptography;
using System.Text;

namespace EduCollab.Application.Services.Auth
{
    public static class RefreshTokenGenerator
    {
        public static string Create()
        {
            var bytes = new byte[32];
            RandomNumberGenerator.Fill(bytes);
            var plaintext = Base64UrlEncode(bytes);
            return plaintext;
        }

        public static string HashPlaintext(string plaintext)
        {
            var hash = SHA256.HashData(Encoding.UTF8.GetBytes(plaintext));
            return Convert.ToHexString(hash);
        }

        private static string Base64UrlEncode(byte[] data)
        {
            return Convert.ToBase64String(data)
                .TrimEnd('=')
                .Replace("+", "-", StringComparison.Ordinal)
                .Replace("/", "_", StringComparison.Ordinal);
        }
    }
}
