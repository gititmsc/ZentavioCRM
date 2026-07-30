using System.Security.Cryptography;
using System.Text;
using ZentavioCRM.Core.Interfaces;

namespace ZentavioCRM.Infrastructure.Security
{
    /// <inheritdoc cref="ISecureTokenGenerator"/>
    public class SecureTokenGenerator : ISecureTokenGenerator
    {
        private const int TokenSizeBytes = 32; // 256 bits

        public string GenerateRawToken()
            => Convert.ToBase64String(RandomNumberGenerator.GetBytes(TokenSizeBytes))
                .Replace('+', '-')
                .Replace('/', '_')
                .TrimEnd('=');

        public string Hash(string rawToken)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));
            return Convert.ToHexString(bytes);
        }
    }
}
